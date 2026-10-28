using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace CM3D2.XtMasterSlave.Plugin
{
    /// <summary>
    ///     Config/*.ini 的读写器，用来替代 PluginExt.SharedConfig。
    ///
    ///     为什么不能继续用 PluginExt.SharedConfig：
    ///     libs/PluginExt.dll 内部走的是 Win32 GetPrivateProfileStringW /
    ///     GetPrivateProfileSectionW / GetPrivateProfileSectionNamesW（CharSet.Auto），
    ///     这套 API 只会按「系统 ANSI 代码页」解码文件，完全不认 UTF-8。结果：
    ///       · 日文系统(CP932)：读 UTF-8 配置 → 所有日文值变乱码
    ///         （issue #2 的「表情がありません。&lt;乱码&gt;」就是这个）
    ///       · 其它代码页(CP936/CP1252 等)：UTF-8 的行尾字节会和 CR 被当成一个
    ///         双字节字符吃掉，解析出空行，GetKeys() 里 Substring(0, -1) 抛
    ///         ArgumentOutOfRangeException（issue #3 的加载崩溃）
    ///     所以「把配置转回 Shift-JIS」只在日文系统上有效，非日文系统照样乱码。
    ///
    ///     本实现：
    ///       · 读：UTF-8（带/不带 BOM）优先，严格解码失败则回退 CP932(Shift-JIS)
    ///       · 写：统一 UTF-8 with BOM
    ///       · 语义与 PluginExt 对齐：section/key 大小写不敏感、值去掉首尾空白与引号、
    ///         "Section::Key" 表示子段、数组/List/Dictionary 展开成数字/名字子段
    ///       · 写回时只替换目标 key 那一行，保留原文件的注释、空行和顺序
    /// </summary>
    internal static class XtIni
    {
        const string Separator = "::";

        static readonly Encoding Utf8Strict = new UTF8Encoding(false, true);
        static Encoding _sjis;

        static Encoding Sjis
        {
            get
            {
                if (_sjis == null)
                {
                    try { _sjis = Encoding.GetEncoding(932); }
                    catch { _sjis = Encoding.Default; }
                }
                return _sjis;
            }
        }

        #region 文档模型

        sealed class IniDoc
        {
            public readonly List<string> Lines = new List<string>();

            // section -> key -> value，两者都忽略大小写（Win32 profile API 的行为）
            public readonly Dictionary<string, Dictionary<string, string>> Sections =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            public string Path;

            public void EnsureSection(string section)
            {
                if (!Sections.ContainsKey(section))
                    Sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        #endregion

        #region 编码

        /// <summary>按 BOM → 严格 UTF-8 → CP932 的顺序探测编码并读出全部文本。</summary>
        static string ReadAllTextAuto(string path, out Encoding encoding)
        {
            byte[] bytes = File.ReadAllBytes(path);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = new UTF8Encoding(true);
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }

            // 无 BOM：先按严格 UTF-8 试，失败说明是 ANSI(Shift-JIS 等)
            try
            {
                string utf8 = Utf8Strict.GetString(bytes);
                encoding = new UTF8Encoding(false);
                return utf8;
            }
            catch (DecoderFallbackException)
            {
            }

            encoding = Sjis;
            return Sjis.GetString(bytes);
        }

        #endregion

        #region 解析

        static IniDoc Parse(string path)
        {
            var doc = new IniDoc { Path = path };
            if (!File.Exists(path))
                return doc;

            Encoding enc;
            string text = ReadAllTextAuto(path, out enc);

            doc.Lines.AddRange(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));

            string current = null;
            foreach (string raw in doc.Lines)
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;
                if (line[0] == ';') // 注释（与 Win32 profile API 一致，只认 ';'）
                    continue;

                if (line[0] == '[')
                {
                    int close = line.IndexOf(']');
                    if (close > 0)
                    {
                        current = line.Substring(1, close - 1).Trim();
                        doc.EnsureSection(current);
                    }
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) // 没有 '=' 的行直接忽略，绝不抛异常
                    continue;
                if (current == null) // 第一个 section 之前的内容忽略
                    continue;

                string key = line.Substring(0, eq).Trim();
                string value = Unquote(line.Substring(eq + 1).Trim());

                Dictionary<string, string> dict = doc.Sections[current];
                if (!dict.ContainsKey(key)) // 同名 section 重复出现时先到先得
                    dict[key] = value;
            }

            return doc;
        }

        /// <summary>Win32 profile API 会把值两端的引号去掉，这里保持一致。</summary>
        static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                return value.Substring(1, value.Length - 2);
            return value;
        }

        static string GetValue(IniDoc doc, string section, string key, string def)
        {
            Dictionary<string, string> dict;
            if (!doc.Sections.TryGetValue(section, out dict))
                return def;
            string value;
            return dict.TryGetValue(key, out value) ? value : def;
        }

        /// <summary>取某个 section 下所有 key（不含子段）。</summary>
        static List<string> GetKeys(IniDoc doc, string section)
        {
            Dictionary<string, string> dict;
            if (!doc.Sections.TryGetValue(section, out dict))
                return new List<string>();
            return new List<string>(dict.Keys);
        }

        /// <summary>
        ///     取某个 section 下的 key，外加形如 "section::子段" 的一层子段名。
        ///     对应 PluginExt 的 IniFile.GetKeyAndSections。
        /// </summary>
        static List<string> GetKeyAndSections(IniDoc doc, string section)
        {
            List<string> keys = GetKeys(doc, section);

            string parent = section + Separator;
            int len = parent.Length;
            foreach (string name in doc.Sections.Keys)
            {
                if (name.Length > len &&
                    name.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
                    keys.Add(name.Substring(len));
            }
            return keys;
        }

        static List<string> GetNumericKeys(IniDoc doc, string section)
        {
            var keys = new List<string>();
            foreach (string k in GetKeyAndSections(doc, section))
            {
                int n;
                if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                    keys.Add(k);
            }
            keys.Sort(delegate(string a, string b)
            {
                return int.Parse(a, CultureInfo.InvariantCulture)
                    .CompareTo(int.Parse(b, CultureInfo.InvariantCulture));
            });
            return keys;
        }

        #endregion

        #region 读取

        /// <summary>等价于 PluginExt.SharedConfig.ReadConfig&lt;T&gt;。</summary>
        public static T ReadConfig<T>(string section, string filename) where T : new()
        {
            string filepath = Path.Combine(PluginExt.SharedConfig.DataPath, filename);
            return Read<T>(section, filepath);
        }

        public static T Read<T>(string section, string filepath) where T : new()
        {
            var value = (T)Activator.CreateInstance(typeof(T));
            IniDoc doc = Parse(filepath);

            foreach (FieldInfo field in typeof(T).GetFields())
            {
                object def = field.GetValue(value);
                field.SetValue(value, ReadObject(doc, field.FieldType, section, field.Name, def));
            }

            MethodInfo onPost = typeof(T).GetMethod("OnPostDeserialize");
            if (onPost != null)
                onPost.Invoke(value, null);

            return value;
        }

        static object ReadObject(IniDoc doc, Type t, string section, string key, object def)
        {
            // 与 PluginExt 一致：只有非空字符串才作为默认值传下去
            string defStr = string.IsNullOrEmpty(def as string) ? "" : def.ToString();
            string text = GetValue(doc, section, key, defStr);

            if (t == typeof(string))
                return text;

            if (t.IsEnum)
            {
                try { return Enum.Parse(t, text); }
                catch { return def; }
            }

            MethodInfo tryParse = t.GetMethod("TryParse", new[] { typeof(string), t.MakeByRefType() });
            if (tryParse != null)
            {
                object[] args = { text, def };
                if ((bool)tryParse.Invoke(null, args))
                    return args[1];
                return def;
            }

            if (t.IsArray)
                return ReadArray(doc, t.GetElementType(), section, key, def);

            if (t.IsGenericType)
            {
                Type defType = t.GetGenericTypeDefinition();
                if (defType == typeof(Dictionary<, >))
                    return ReadDictionary(doc, t.GetGenericArguments()[1], section, key, def);
                if (defType == typeof(List<>))
                    return ReadList(doc, t.GetGenericArguments()[0], section, key, def);
            }

            return def;
        }

        static object ReadArray(IniDoc doc, Type elem, string section, string key, object def)
        {
            string sec = section + Separator + key;
            List<string> keys = GetNumericKeys(doc, sec);
            if (keys.Count == 0)
                return def; // 文件里没写 → 用字段自带的默认值

            Array result = Array.CreateInstance(elem, keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                object d = elem.IsValueType ? Activator.CreateInstance(elem) : null;
                result.SetValue(ReadObject(doc, elem, sec, keys[i], d), i);
            }
            return result;
        }

        static object ReadList(IniDoc doc, Type elem, string section, string key, object def)
        {
            string sec = section + Separator + key;
            List<string> keys = GetNumericKeys(doc, sec);
            if (keys.Count == 0)
                return def;

            IList result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elem));
            foreach (string k in keys)
            {
                object d = elem.IsValueType ? Activator.CreateInstance(elem) : null;
                result.Add(ReadObject(doc, elem, sec, k, d));
            }
            return result;
        }

        static object ReadDictionary(IniDoc doc, Type valType, string section, string key, object def)
        {
            string sec = section + Separator + key;
            List<string> keys = GetKeys(doc, sec);
            if (keys.Count == 0)
                return def;

            IDictionary result =
                (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), valType));
            foreach (string k in keys)
            {
                object d = valType.IsValueType ? Activator.CreateInstance(valType) : null;
                result[k] = ReadObject(doc, valType, sec, k, d);
            }
            return result;
        }

        #endregion

        #region 写入

        /// <summary>等价于 PluginExt.SharedConfig.SaveConfig。</summary>
        public static void SaveConfig<T>(string section, string filename, T data)
        {
            string filepath = Path.Combine(PluginExt.SharedConfig.DataPath, filename);
            Save(section, data, filepath);
        }

        public static void Save<T>(string section, T data, string filepath)
        {
            IniDoc doc = Parse(filepath);

            MethodInfo onPre = typeof(T).GetMethod("OnPreSerialize");
            if (onPre != null)
                onPre.Invoke(data, null);

            foreach (FieldInfo field in typeof(T).GetFields())
                WriteObject(doc, field.FieldType, field.GetValue(data), section, field.Name);

            Flush(doc);
        }

        static void WriteObject(IniDoc doc, Type t, object data, string section, string key)
        {
            string str = data == null ? "" : data.ToString();

            if (t.IsEnum || t.GetMethod("TryParse", new[] { typeof(string), t.MakeByRefType() }) != null ||
                t == typeof(string))
            {
                SetKey(doc, section, key, str);
                return;
            }

            if (t.IsArray)
            {
                string sec = section + Separator + key;
                int i = 0;
                foreach (object item in (Array)data)
                {
                    WriteObject(doc, t.GetElementType(), item, sec, i.ToString(CultureInfo.InvariantCulture));
                    i++;
                }
                return;
            }

            if (t.IsGenericType)
            {
                Type defType = t.GetGenericTypeDefinition();
                if (defType == typeof(Dictionary<, >))
                {
                    string sec = section + Separator + key;
                    Type valType = t.GetGenericArguments()[1];
                    foreach (DictionaryEntry entry in (IDictionary)data)
                        WriteObject(doc, valType, entry.Value, sec, entry.Key.ToString());
                    return;
                }
                if (defType == typeof(List<>))
                {
                    string sec = section + Separator + key;
                    Type elem = t.GetGenericArguments()[0];
                    int i = 0;
                    foreach (object item in (IEnumerable)data)
                    {
                        WriteObject(doc, elem, item, sec, i.ToString(CultureInfo.InvariantCulture));
                        i++;
                    }
                    return;
                }
            }
        }

        /// <summary>找到 [section] 的起始行，并给出该段结束行（下一个 section 或文件末尾）。</summary>
        static int FindSectionLine(List<string> lines, string section, out int endLine)
        {
            endLine = lines.Count;
            int start = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.Length == 0 || t[0] != '[')
                    continue;
                int close = t.IndexOf(']');
                if (close <= 0)
                    continue;

                if (start >= 0)
                {
                    endLine = i;
                    break;
                }

                string name = t.Substring(1, close - 1).Trim();
                if (string.Equals(name, section, StringComparison.OrdinalIgnoreCase))
                    start = i;
            }

            return start;
        }

        static void SetKey(IniDoc doc, string section, string key, string value)
        {
            string newLine = key + "=" + value;
            int end;
            int start = FindSectionLine(doc.Lines, section, out end);

            if (start < 0)
            {
                // 整个 section 都不存在 → 追加到文件末尾
                if (doc.Lines.Count > 0 && doc.Lines[doc.Lines.Count - 1].Trim().Length != 0)
                    doc.Lines.Add("");
                doc.Lines.Add("[" + section + "]");
                doc.Lines.Add(newLine);
                doc.EnsureSection(section);
                doc.Sections[section][key] = value;
                return;
            }

            for (int i = start + 1; i < end; i++)
            {
                string t = doc.Lines[i].Trim();
                if (t.Length == 0 || t[0] == ';')
                    continue;
                int eq = t.IndexOf('=');
                if (eq <= 0)
                    continue;
                if (!string.Equals(t.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase))
                    continue;

                doc.Lines[i] = newLine; // 原地替换，保留缩进之外的一切
                doc.Sections[section][key] = value;
                return;
            }

            // key 不存在 → 插到本段最后一条内容之后（跳过段尾空行）
            int insertAt = end;
            while (insertAt > start + 1 && doc.Lines[insertAt - 1].Trim().Length == 0)
                insertAt--;

            doc.Lines.Insert(insertAt, newLine);
            doc.Sections[section][key] = value;
        }

        static void Flush(IniDoc doc)
        {
            string dir = System.IO.Path.GetDirectoryName(doc.Path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 去掉末尾空行再统一补一个换行，否则每保存一次就会多累积一个空行
            int last = doc.Lines.Count - 1;
            while (last >= 0 && doc.Lines[last].Trim().Length == 0)
                last--;

            var sb = new StringBuilder();
            for (int i = 0; i <= last; i++)
                sb.Append(doc.Lines[i]).Append("\r\n");

            // 统一写成 UTF-8 with BOM：记事本能正确识别，本读取器也能读
            File.WriteAllText(doc.Path, sb.ToString(), new UTF8Encoding(true));
        }

        #endregion
    }
}

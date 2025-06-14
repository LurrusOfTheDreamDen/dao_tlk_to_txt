/*
 * for Handling "Dragon Age: Origin" Talktables
 * - File format // GFF 4.0, TLK V0.2
 * 
 * Use for free, but the risk is your responsibility.
 * 
 * History.
 * 
 * 2009-12-09
 * added three method: 
 *  1. List<TlkItemPair> Diff(GffTlkFile tlk)
 *  2. XmlDocument Export()
 *  3. int Import(XmlDocument)
 *  4. minor improve
 * 
 * 2009-12-07
 * first, basic file handling
 */
using System;
using System.Collections.Generic;
using System.Text;

using System.Security;
using System.IO;
using System.Collections;
using System.Xml;

namespace GFF
{
    /// <summary>
    /// helper tlk string item for listing, import, export.
    /// </summary>
    class TlkItem
    {
        public uint RefNo;
        public string Text;

        public TlkItem(uint refno, string text)
        {
            RefNo = refno;
            Text = text;
        }
        public TlkItem(int refno, string text)
        {
            RefNo = (uint)refno;
            Text = text;
        }
        public static int Compare(TlkItem x, TlkItem y)
        {
            return (int)(x.RefNo - y.RefNo);
        }
    }

    class TlkItemPair
    {
        public TlkItem item1;
        public TlkItem item2;

        public TlkItemPair(TlkItem i1, TlkItem i2)
        {
            item1 = i1;
            item2 = i2;
        }
    }

    class TlkElement
    {
        public bool IsValid
        {
            get { return (Offset != 0xFFFFFFFF); }
        }

        /// <summary>
        /// StringRef
        /// </summary>
        public uint StrRef;
        /// <summary>
        /// if unused, 0xFFFFFFFF(-1). if used, File offset - 0x60
        /// </summary>
        public uint Offset;
        /// <summary>
        /// String Data.
        /// </summary>
        public TlkString String;

        public TlkElement(BinaryReader br)
        {
            StrRef = br.ReadUInt32();
            Offset = br.ReadUInt32();
        }
        /// <summary>
        /// For Reconstruct.
        /// </summary>
        public TlkString NewTlkString(BinaryReader br)
        {
            if (Offset == 0xFFFFFFFF) return null;
            //TlkString
            //  uint  Size; (with null char)
            //  string Text; (with null char)
            //  padding 0xFFFFFFFF for dword size
            br.BaseStream.Seek(0x60 + Offset, SeekOrigin.Begin);
            return (String = new TlkString(br));
        }
    }

    class TlkString
    {
        // Used by all consturctor
        static int sSize = 0;
        static char[] sbuf = new char[0x1000];  // 4K

        public string String;
        /// <summary>
        ///  A TlkString is referenced by more than one StrRef.
        /// </summary>
        public List<TlkElement> Elements = new List<TlkElement>();

        public TlkString(BinaryReader br)
        {
            sSize = br.ReadInt32(); // length+NULL
            if (sSize < 0 || sSize >= 1024000/*100K roughly max size*/)
            {
                // Error Handling !!
                return;
            }
            if (sbuf.Length < sSize) sbuf = new char[sSize];
            sSize = br.Read(sbuf, 0, sSize);
            if (sbuf[sSize - 1] == 0) sSize--; // Trim NULL char
            String = new String(sbuf, 0, sSize);
        }
        public TlkString(string value, TlkElement elem)
        {
            this.String = value;
            Elements.Add(elem);
            elem.String = this;
        }
    }

    /// <summary>
    /// Hashtable. Key:StrRef/Offset, Value:TlkString 
    /// </summary>
    class TlkStrTable
    {
        Hashtable RefStrings;

        public int Count { get { return RefStrings.Count; } }

        public TlkStrTable() { RefStrings = new Hashtable(); }
        public TlkStrTable(int capacity) { RefStrings = new Hashtable(capacity); }

        public TlkString this[uint Ref]
        {
            get { return RefStrings[Ref.ToString("X8")] as TlkString; }
            set { RefStrings[Ref.ToString("X8")] = value; } 
        }

        public TlkString this[string Ref]
        {
            get { return RefStrings[Ref.ToUpper()] as TlkString; }
            set { RefStrings[Ref.ToUpper()] = value; }
        }

        public void Clear()
        {
            RefStrings.Clear();
        }
    }

    /// <summary>
    /// DAO GFF 4.0 TLK File 
    /// </summary>
    class GffTlkFile
    {
        #region Error Handling Routine

        public enum ERROR
        {
            OK,
            FILE_NOT_FOUND,
            FILE_SECURITY,
            FILE_ACCESS,
            FILE_LENGTH,
            HEADER,
            ELEMEMENT,
            IO,
            UNKNOWN,
        }

        public delegate void LogOutHandler(string log);
        public LogOutHandler OnLogOut;

        ERROR _ErrorCode = ERROR.OK;
        string _LastLogMessage = String.Empty;
        /// <summary>
        /// store errors when Load/Save and etc...
        /// </summary>
        public ERROR Error
        {
            get { return _ErrorCode; }
            set { _ErrorCode = value; }
        }
        public bool HasError
        {
            get { return _ErrorCode != ERROR.OK; }
        }
        public string LastLogMessage
        {
            get { return _LastLogMessage; }
        }

        void ErrorClear() {
            _ErrorCode = ERROR.OK;
            _LastLogMessage = String.Empty;
        }
        void LogOut(ERROR errorCode, string logText)
        {
            if (errorCode != ERROR.OK)
                _ErrorCode = errorCode;
            _LastLogMessage = logText;
            if (OnLogOut != null) OnLogOut(_LastLogMessage);
        }

        #endregion

        #region Fields

        public const int HEADER_LENGTH = 100; //20+80;
        public const int HEADER_LENGTH_PLUS_LISTCOUNT = 104; //20+80+4;

        public static readonly byte[] FirstHeader
            = Encoding.ASCII.GetBytes("GFF V4.0PC  TLK V0.2");
        //byte[] FileType;   // "GFF " // 4-char file type string
        //byte[] FileVersion;// "V4.0" // 4-char GFF Version. At the time of writing, the version is "V3.2"
        //byte[] Platform;   // "PC  "
        //byte[] SubType;    // "TLK "
        //byte[] SubVersion; // "V0.2"
        public readonly byte[] SecondHeader = new byte[80];
        //uint unknown1;      // 00000002
        //uint unknown2;      // 00000060
        //byte[] unknown3;    // "TLK "
        //uint unknown4;      // 00000001
        //uint unknown5;      // 0000003C
        //uint unknown6;      // 00000004
        //byte[] unknown7;    // "STRN"
        //uint unknown6;      // 00000002
        //uint unknown6;      // 00000048
        //uint unknown6;      // 00000008
        //uint unknown6;      // 00004A39
        //uint unknown6;      // 00C00001
        //uint unknown6;      // 00000000
        //uint unknown6;      // 00004A3A
        //uint unknown6;      // 00000004
        //uint unknown6;      // 00000000
        //uint unknown6;      // 00004A3A
        //uint unknown6;      // 0000000E
        //uint unknown6;      // 00000004
        //uint unknown6;      // 00000004
        // offset 0x64
        public uint ElementCount = 0;      // Core(0x00010C44) // Element LIST COUNT
        
        // Header // GffHeader
        public readonly List<TlkElement> Elements = new List<TlkElement>();
        List<TlkString> Strings = new List<TlkString>();

        public List<TlkString> TlkStrings { get { return Strings; } }

        // helpers
        public readonly TlkStrTable RefTable = new TlkStrTable();
        public readonly TlkStrTable OffsetTable = new TlkStrTable();

        #endregion

        string _FileName = String.Empty;

        /// <summary>
        /// GFF 4.0 Tlk FileName
        /// </summary>
        public string FileName
        {
            get { return Path.GetFileName(_FileName); }
        }

        public GffTlkFile()
        {
        }
        
        public void Load(string fileName)
        {
            ErrorClear();

            ElementCount = 0;
            _FileName = String.Empty;
            Elements.Clear();
            Strings.Clear();
            RefTable.Clear();
            OffsetTable.Clear();

            FileStream fs = null;
            BinaryReader br = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                if (fs.Length <= (HEADER_LENGTH_PLUS_LISTCOUNT))
                {
                    LogOut(ERROR.FILE_LENGTH, "Load Failed, DAO TLK file length invalid");
                    return;
                }
                br = new BinaryReader(fs, Encoding.Unicode);

                byte[] buf = new byte[FirstHeader.Length];

                int read = br.Read(buf, 0, FirstHeader.Length);
                if ( ! Compare(FirstHeader, FirstHeader.Length, buf, read) )
                {
                    LogOut(ERROR.HEADER, "Load Failed, DAO Header invalid");
                    return;
                }
                read = br.Read(SecondHeader, 0, SecondHeader.Length);
                if(read != SecondHeader.Length)
                {
                    LogOut(ERROR.HEADER, "Load Failed, DAO Header incomplete");
                    return;
                }
                // read element index count (0x64 = 100)
                ElementCount = br.ReadUInt32();
                if (fs.Length <= ((ElementCount * 16) + HEADER_LENGTH_PLUS_LISTCOUNT))
                {
                    LogOut(ERROR.ELEMEMENT, "Load Failed, Element Count mismatch file size");
                    return;
                }
                Elements.Capacity = (int)ElementCount;
                // offset 0x68, 104
                for(uint i=0; i < ElementCount; i++)
                {
                    Elements.Add(new TlkElement(br));
                }
                // offset (count, 0x10c44 => *8 = 0x86220 = 549408)
                // read strings
                uint offset = (uint)(fs.Position-0x60);
                uint oEnd = (uint)(fs.Length-4-0x60);
                while (offset < oEnd)
                {
                    TlkString tlkString = new TlkString(br);
                    if (tlkString.String == null) break; // Error
                    Strings.Add(tlkString);

                    OffsetTable[offset] = tlkString;

                    offset = (uint)(fs.Position - 0x60);
                    if ((offset+4) >= oEnd)
                    {
                        break;
                    }
                    else if ((tlkString.String.Length % 2) == 0) // skip dword size padding
                    {
                        br.ReadInt16();
                        offset += 2;
                    }
                }
                bool notFoundOffsets = false;
                for (int i = 0; i < Elements.Count; i++)
                {
                    TlkElement item = Elements[i];
                    if (item.IsValid)
                    {
                        TlkString tlkString = OffsetTable[item.Offset];
                        if (tlkString == null)
                        {
                            notFoundOffsets = true;
                            break;
                        }
                        else
                        {
                            item.String = tlkString;
                            tlkString.Elements.Add(item);
                            RefTable[item.StrRef] = tlkString;
                        }
                    }
                }
                if (notFoundOffsets)
                {
                    // Invalid String Entries !!!
                    // Reconstruct !!!
                    string filename = Path.GetFileName( fileName);
                    LogOut(ERROR.OK, "File Load :: " + filename + " :: Invalid String Entries :: Reconstructing !!!");

                    Strings.Clear();
                    OffsetTable.Clear();
                    RefTable.Clear();
                    foreach (TlkElement item in Elements)
                    {
                        // header 20(SIGN)+80(INFO)
                        // index offset 86228 => offset from list (start counting)
                        // real  offset 86288 (index+0x60=100)
                        
                        //item.Offset
                        if (item.Offset != 0xFFFFFFFF)
                        {
                            TlkString tlkstring = OffsetTable[item.Offset];
                            if (tlkstring == null)
                            {
                                tlkstring = item.NewTlkString(br);
                                Strings.Add(tlkstring);
                                OffsetTable[item.Offset] = tlkstring;
                            }
                            else
                            {
                                item.String = tlkstring;
                            }
                            tlkstring.Elements.Add(item);
                            RefTable[item.StrRef] = tlkstring;
                        }
                    }
                }
                _FileName = fileName;
            }
            catch (FileNotFoundException ex)
            {
                LogOut(ERROR.FILE_NOT_FOUND, "Load Failed, FileNotFound\r\n  " + ex.Message);
            }
            catch (SecurityException ex)
            {
                LogOut(ERROR.FILE_SECURITY, "Load Failed, Security\r\n  " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogOut(ERROR.FILE_ACCESS, "Load Failed, UnauthorizedAccess\r\n  " + ex.Message);
            }
            catch (IOException ex)
            {
                LogOut(ERROR.IO, "Load Failed, IOException\r\n  " + ex.Message);
            }
            catch (Exception ex)
            {
                LogOut(ERROR.UNKNOWN, "Load Failed, Exception\r\n  " + ex.Message);
            }
            finally
            {
                if (fs != null) fs.Close();
                if (br != null ) br.Close();
            }
        }
        
        /// <summary>
        /// TlkElement.Offset fixed. And OffsetTable is rebuilt.
        /// </summary>
        void RebuildElementOffset()
        {
            OffsetTable.Clear();
            uint offset = 0x8/*Unknown DWORD + ElementCount*/ + (uint)Elements.Count*8;
            for (int i = 0; i < Strings.Count; i++)
            {
                TlkString item = Strings[i];
                // Fix OffsetTalbe
                OffsetTable[offset] = item;
                // Fix TlkElement Offset
                foreach (TlkElement elem in item.Elements)
                {
                    elem.Offset = offset;
                }
                offset += 0x4/*Size*/ + (uint)(item.String.Length + 1) * 2;
                if (item.String.Length % 2 == 0) offset += 2; // dword padding
            }
        }

        /// <summary>
        /// Reconstruct internal TlkString list (for merging dup-strings).
        /// This method changes all infos of GffTlkFile.
        /// - Strings and TlkString.Elements list
        /// - TlkElement.Offset and OffsetTable
        /// - RefTable
        /// </summary>
        public int ReconstructStrings()
        {
            List<TlkString> newStrings = new List<TlkString>();

            int mergedStringCount = 0;
            int mergedElementCount = 0;
            Hashtable StrTable = new Hashtable();
            for (int i = 0; i < Strings.Count; i++)
            {
                TlkString tlkstr = Strings[i];
                TlkString tlkstring = StrTable[tlkstr.String] as TlkString;
                if (tlkstring == null) // use unique string
                {
                    newStrings.Add(tlkstr);
                    StrTable[tlkstr.String] = tlkstr;
                }
                else
                {
                    mergedStringCount ++;
                    mergedElementCount += tlkstr.Elements.Count;

                    // Discard duplicated TlkString, and
                    // Append owned element list to Unique TlkString
                    foreach (TlkElement elem in tlkstr.Elements)
                    {
                        elem.String = tlkstring; // change!! tlkstring refrence
                        tlkstring.Elements.Add(elem);
                    }
                }
            }
            if (mergedStringCount > 0 || mergedElementCount > 0)
            {
                Strings = newStrings; // Change!!
                RebuildElementOffset();
                // Rebuild RefTable
                foreach (TlkElement item in Elements)
                    if (item.Offset != 0xFFFFFFFF)
                        RefTable[item.StrRef] = item.String;

                LogOut(ERROR.OK, "Reconstructed, "
                    + mergedStringCount.ToString() + " TlkString(s) and "
                    + mergedElementCount.ToString() + " TlkElement(s) Merged");

                StrTable = null;
                GC.Collect();
            }
            else
            {
                LogOut(ERROR.OK, "Not reconstructed, no need !!");
            }
            return mergedStringCount;
        }

        /// <summary>
        /// Save Tlk File. All offset is rebuilt.
        /// </summary>
        public void SaveAs(string fileName)
        {
            ErrorClear();

            FileStream fs = null;
            BinaryWriter bw = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                bw = new BinaryWriter(fs, Encoding.Unicode);

                bw.Write(FirstHeader, 0, FirstHeader.Length);
                bw.Write(SecondHeader, 0, SecondHeader.Length);
                bw.Write(ElementCount);
                
                // Fix Offset
                RebuildElementOffset();

                // offset 0x68, 104
                for (int i = 0; i < ElementCount; i++)
                {
                    TlkElement item = Elements[i];
                    bw.Write(item.StrRef);
                    bw.Write(item.Offset);
                }
                // offset (count, 0x10c44 => *8 = 0x86220 = 549408)
                // write strings
                int upBound = Strings.Count;
                for (int i = 0; i < Strings.Count; i++)
                {
                    TlkString item = Strings[i];
                    bw.Write((uint)(item.String.Length + 1));
                    bw.Write(item.String.ToCharArray());
                    bw.Write((char)0);
                    if ((--upBound > 0) && ((item.String.Length % 2) == 0)) bw.Write((short)-1);
                }
                bw.Write((byte)0x0a);
                _FileName = fileName;
            }
            catch (SecurityException ex)
            {
                LogOut(ERROR.FILE_SECURITY, "Save Failed, Security\r\n  " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogOut(ERROR.FILE_ACCESS, "Save Failed, UnauthorizedAccess\r\n  " + ex.Message);
            }
            catch (IOException ex)
            {
                LogOut(ERROR.IO, "Save Failed, IOException\r\n  " + ex.Message);
            }
            catch (Exception ex)
            {
                LogOut(ERROR.UNKNOWN, "Save Failed, Exception\r\n  " + ex.Message);
            }
            finally
            {
                if (fs != null) fs.Close();
                if (bw != null) bw.Close();
            }
        }

        /// <summary>
        /// Update StrRef's String.
        /// If Separate == true, (multi-referenced) TlkString is separated to two TlkString.
        /// Else if Separate == false, updated simply.
        /// </summary>
        public TlkString UpdateString(TlkString tlkstr, uint strref, string text, bool Separate)
        {
            if (!Separate || tlkstr.Elements.Count == 1)
            {
                // unique, separating is no nead.
                tlkstr.String = text;
                return tlkstr;
            }
            TlkElement elem = null;
            for (int i = 0; i < tlkstr.Elements.Count; i++)
            {
                if (tlkstr.Elements[i].StrRef == strref)
                {
                    elem = tlkstr.Elements[i];
                    tlkstr.Elements.RemoveAt(i);
                    break;
                }
            }
            if (elem == null) return null;

            // make unique offset
            elem.Offset = (uint)(tlkstr.Elements[tlkstr.Elements.Count - 1].Offset)
                + (uint)tlkstr.Elements.Count;
            TlkString newTlkStr = new TlkString(text, elem);

            // Update GffTlkFile Tables
            this.Strings.Add(newTlkStr);
            this.OffsetTable[elem.Offset] = newTlkStr;
            this.RefTable[elem.StrRef] = newTlkStr;
            return newTlkStr;
        }

        /// <summary>
        /// Special chars(\r\n ...) replaced, and import.
        /// </summary>
        public int Import(List<TlkItem> list, bool Separate)
        {
            int changed = 0;
            TlkString str1;
            TlkString str2;
            foreach (TlkItem item in list)
            {
                str1 = RefTable[item.RefNo];
                if (str1 != null)
                {
                    if (item.Text.IndexOf("\\n") >= 0)
                        str2 = UpdateString(str1, item.RefNo, item.Text.Replace("\\n", "\r\n"), Separate);
                    else
                        str2 = UpdateString(str1, item.RefNo, item.Text, Separate);
                    if( str2 != null )
                        changed ++;
                }
            }
            return changed;
        }

        /// <summary>
        /// Import.
        /// </summary>
        public int Import(XmlDocument doc, bool Separate)
        {
            int changed = 0;
            XmlElement root = doc.DocumentElement;
            uint nref = 0;
            for (XmlElement elem = root.FirstChild as XmlElement; elem != null; elem = elem.NextSibling as XmlElement)
            {
                for (XmlNode xref = elem.FirstChild; xref != null; xref = xref.NextSibling)
                {
                    string strref = xref.InnerText;
                    if (UInt32.TryParse(strref, out nref))
                    {
                        TlkString tlkstr = RefTable[nref] as TlkString;
                        if (tlkstr != null)
                        {
                            tlkstr = UpdateString(tlkstr, nref, elem.GetAttribute("value"), Separate);
                            if (tlkstr != null)
                            {
                                changed++;
                                if (Separate) break;
                            }
                        }
                    }
                }
            }
            return changed;
        }

        /// <summary>
        /// Export String items. See param.
        /// </summary>
        /// <param name="Unique">if not true, one item on each StrRef. Else unique strings.</param>
        /// <param name="ReplaceNL">if not true, preserve orginal string. Else replace NL(\r\n) to '\'+'n'.</param>
        public List<TlkItem> Export(bool Unique, bool ReplaceNL)
        {
            List<TlkItem> list = new List<TlkItem>();
            for (int i = 0; i < ElementCount; i++)
            {
                TlkElement item = Elements[i];
                if (item.IsValid)
                {
                    if ( !Unique || item.String.Elements[0].StrRef == item.StrRef)
                    {
                        list.Add(
                            new TlkItem(item.StrRef,
                                ReplaceNL ? item.String.String.Replace("\r\n","\\n") : item.String.String)
                            );
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Export strings to xml form
        /// </summary>
        public XmlDocument Export()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><GffTlk></GffTlk>");
            
            XmlElement GffTlk = doc.DocumentElement;
            GffTlk.SetAttribute("FileName", FileName);
            GffTlk.SetAttribute("strCount", Strings.Count.ToString());
            GffTlk.SetAttribute("refCount", ElementCount.ToString());
            int nCount = 0;
            foreach (TlkString tlkstr in Strings)
            {
                XmlElement elm = doc.CreateElement("str");
                elm.SetAttribute("value", tlkstr.String);
                elm.SetAttribute("refs", tlkstr.Elements.Count.ToString());
                foreach(TlkElement tlkelm in tlkstr.Elements)
                {
                    XmlElement elm2 = doc.CreateElement("ref");
                    elm2.InnerText = tlkelm.StrRef.ToString();
                    elm.AppendChild(elm2);
                    nCount++;
                }
                GffTlk.AppendChild(elm);
            }
            GffTlk.SetAttribute("refValidCount", nCount.ToString());
            return doc;
        }

        /// <summary>
        /// Compare with the other tlk. return difference items and count.
        /// </summary>
        public List<TlkItemPair> Diff(GffTlkFile tlk)
        {
            List<TlkItem> list1 = this.Export(false, false);
            List<TlkItem> list2 = tlk.Export(false, false);
            list1.Sort(TlkItem.Compare);
            list2.Sort(TlkItem.Compare);

            List<TlkItemPair> listDiff = new List<TlkItemPair>();

            int i = 0;
            int j = 0;
            for (; i < list1.Count && j < list2.Count;)
            {
                if (list1[i].RefNo < list2[j].RefNo)
                {
                    listDiff.Add(new TlkItemPair(list1[i++], null));
                }
                else if (list1[i].RefNo == list2[j].RefNo)
                {
                    if (list1[i].Text.CompareTo(list2[j].Text) != 0)
                    {
                        listDiff.Add(new TlkItemPair(list1[i], list2[j]));
                    }
                    i++;
                    j++;
                }
                else
                {
                    listDiff.Add(new TlkItemPair(null, list2[j++]));
                }
            }
            // remains list1, deleted StrRef(s)
            while (i < list1.Count)
            {
                listDiff.Add(new TlkItemPair(list1[i++], null));
            }
            // or
            // remains list2, added StrRef(s)
            while (j < list2.Count)
            {
                listDiff.Add(new TlkItemPair(null, list2[j++]));
            }
            return listDiff;
        }

        //-------------------------- Static Methods ----------------------------
        // Compare(byte[], byte[])

        #region Static Methods

        public static bool Compare(byte[] A, int lenA, byte[] B, int lenB)
        {
            if (lenA != lenB) return false;
            for (int i = 0; i < lenA; i++)
            {
                if (A[i] != B[i]) return false;
            }
            return true;
        }

        #endregion
    }
}

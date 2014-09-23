using System;
using System.Text;
using System.IO;
using Shell32;
using System.Collections;

namespace DetailFileInfo
{
    /// <summary> 
    /// Returns the detailed Information of a given file. 
    /// </summary> 
    public class CFileInfo
    {

        

        public CFileInfo(string sFPath, FileAttributes[] desiredAttributes = null)
        {

            // check if the given File exists 
            if (File.Exists(sFPath))
            {

                ArrayList aDetailedInfo = new ArrayList();

                FileInfo oFInfo = new FileInfo(sFPath);

                FileName = oFInfo.Name;
                FullFileName = oFInfo.FullName;
                FileExtension = oFInfo.Extension;
                FileSize = oFInfo.Length;
                FilePath = oFInfo.Directory.ToString();
                FileCreationDate = oFInfo.CreationTime;
                FileModificationDate = oFInfo.LastWriteTime;

                #region "read File Details"

                aDetailedInfo = GetDetailedFileInfo(sFPath, desiredAttributes);

                foreach (DetailedFileInfo oDFI in aDetailedInfo)
                {
                    switch ((FileAttributes)oDFI.ID)
                    {
                        case FileAttributes.ItemType:
                            FileType = oDFI.Value;
                            break;
                        case FileAttributes.Authors:
                            FileAuthor = oDFI.Value;
                            break;
                        case FileAttributes.Title:
                            FileTitle = oDFI.Value;
                            break;
                        case FileAttributes.Subject:
                            FileSubject = oDFI.Value;
                            break;
                        case FileAttributes.Categories:
                            FileCategory = oDFI.Value;
                            break;
                        case FileAttributes.Comments:
                            FileComment = oDFI.Value;
                            break;
                        case FileAttributes.DateTaken:
                            DateTaken = ParseDate(oDFI.Value);
                            break;
                        case FileAttributes.PerceivedType:
                            PerceivedType = ParsePerceivedFileType(oDFI.Value);
                            break;
                        case FileAttributes.CameraModel:
                            CameraModel = oDFI.Value;
                            break;
                        case FileAttributes.CameraMaker:
                            CameraMake = oDFI.Value;
                            break;
                        default:
                            break;
                    }

                }


                #endregion


            }
            else
            {
                throw new Exception("The given File does not exist");
            }

        }

        private DateTime? ParseDate(string p)
        {
            if (string.IsNullOrWhiteSpace(p))
                return null;

            p = CleanOrderMarks(p);

            DateTime output;

            const string shellDateFormat = "g";
            if (DateTime.TryParseExact(p, shellDateFormat, System.Globalization.CultureInfo.CurrentUICulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out output))
                return output;

            Console.WriteLine("Couldn't understand date format: " + p);
            return null;
        }

        private PerceivedFileType ParsePerceivedFileType(string p)
        {
            PerceivedFileType output;
            if (Enum.TryParse<PerceivedFileType>(p, out output))
                return output;

            Console.WriteLine("Unknown perceived format: " + p);
            return PerceivedFileType.Unspecified;
        }

        private string CleanOrderMarks(string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 8206 || input[i] == 8207)
                    continue;
                sb.Append(input[i]);
            }
            return sb.ToString();
        }


        #region "Properties"
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public string FullFileName { get; private set; }
        public string FileExtension { get; private set; }
        public long FileSize { get; private set; }
        public DateTime FileCreationDate { get; private set; }
        public DateTime FileModificationDate { get; private set; }
        public string FileType { get; private set; }
        public string FileTitle { get; private set; }
        public string FileSubject { get; private set; }
        public string FileAuthor { get; private set; }
        public string FileCategory { get; private set; }
        public string FileComment { get; private set; }

        public DateTime? DateTaken { get; set; }
        public PerceivedFileType PerceivedType { get; set; }
        public string CameraModel { get; set; }
        public string CameraMake { get; set; }


        #endregion

        #region "Methods"

        private Shell32.Folder GetShellFolder(string directoryFullName)
        {
            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            object shell = Activator.CreateInstance(shellAppType);

            Shell32.Folder folder = (Shell32.Folder)shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { directoryFullName });
            return folder;
        }

        private ArrayList GetDetailedFileInfo(string sFile, FileAttributes[] valuesToRetrieve)
        {
            if (valuesToRetrieve == null)
            {
                var list = new System.Collections.Generic.List<FileAttributes>();
                for (int i = 0; i < 30; i++)
                {
                    list.Add((FileAttributes)i);
                }
                valuesToRetrieve = list.ToArray();
            }

            ArrayList aReturn = new ArrayList();
            if (sFile.Length > 0)
            {
                try
                {
                    // Creating a ShellClass Object from the Shell32 
                    Folder dir = GetShellFolder(Path.GetDirectoryName(sFile));
                    // Creating a new FolderItem from Folder that includes the File 
                    FolderItem item = dir.ParseName(Path.GetFileName(sFile));
                    // loop throw the Folder Items 
                    foreach (var attribute in valuesToRetrieve)
                    {
                        string det = dir.GetDetailsOf(item, (int)attribute);
                        // Create a helper Object for holding the current Information 
                        // an put it into a ArrayList 
                        DetailedFileInfo oFileInfo = new DetailedFileInfo((int)attribute, det);
                        aReturn.Add(oFileInfo);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return aReturn;
        }
        #endregion
    }


    // Helper Class from holding the detailed File Informations 
    // of the System 
    public class DetailedFileInfo
    {
        int iID = 0;
        string sValue = "";

        public int ID
        {
            get { return iID; }
            set
            {
                iID = value;
            }
        }
        public string Value
        {
            get { return sValue; }
            set { sValue = value; }
        }

        public DetailedFileInfo(int ID, string Value)
        {
            iID = ID;
            sValue = Value;
        }
    }

    public enum FileAttributes
    {
        Name = 0,
        Size = 1,
        ItemType = 2,
        DateModified = 3,
        DateCreated = 4,
        DateAccessed = 5,
        Attributes = 6,
        OfflineStatus = 7,
        OfflineAvailability = 8,
        PerceivedType = 9,
        Owner = 10,
        Kind = 11,
        DateTaken = 12,
        ContributingArtists = 13,
        Album = 14,
        Year = 15,
        Genre = 16,
        Conductors = 17,
        Tags = 18,
        Ratings = 19,
        Authors = 20,
        Title = 21,
        Subject = 22,
        Categories = 23,
        Comments = 24,
        Copyright = 25,
        Number = 26,
        Length = 27,
        BitRate = 28,
        Protected = 29,
        CameraModel = 30,
        Dimensions = 31,
        CameraMaker = 32,
        Company = 33,
        FileDescription = 34,
        ProgramName = 35,
        Duration = 36,
        IsOnline = 37,
        IsRecurring = 38,
        Location = 39,

        ExifVersion = 230,
        EventTitle = 231,
        ExposureBias = 232,
        ExposureProgram = 233,
        ExposureTime = 234,
        FStop = 235,
        FlashMode = 236,
        FocalLength = 237,
        FilmFocalLength = 238,
        ISOSpeed = 239,
        LensMaker = 240,
        LensModel = 241,
        LightSource = 242,
        MaxAperture = 243,
        MeteringMode = 244,
        Orientation = 245,
        People = 246,
        ProgramMode = 247,
        Saturation = 248,
        SubjectDistance = 249,
        WhiteBalance = 250,
        Priority = 251
    }
    public enum PerceivedFileType
    {
        Unspecified,
        Image,
        Video,
    }
}
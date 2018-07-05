using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace photoSorted
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //String path = @"C:\Users\kanda\Downloads";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void browse_folder(object sender, RoutedEventArgs e)
        {            
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String path = "";
                PPB.IsEnabled = true;
                path = fbd.SelectedPath;
                TextPathBox.Text = path;
                //System.Windows.Forms.MessageBox.Show(fbd.SelectedPath);
            }
        }
        public enum ImageFormat
        {
            bmp,
            jpg,
            gif,
            tiff,
            png,
            unknown
        }

        public static ImageFormat GetImageFormat(byte[] bytes)
        {
            // see http://www.mikekunz.com/image_file_header.html  
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon
            var jpeg3 = new byte[] { 255, 216, 255 }; // jpeg

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
                return ImageFormat.bmp;

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
                return ImageFormat.gif;

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat.png;

            if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
                return ImageFormat.tiff;

            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
                return ImageFormat.tiff;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat.jpg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat.jpg;

            if (jpeg3.SequenceEqual(bytes.Take(jpeg3.Length)))
                return ImageFormat.jpg;

            return ImageFormat.unknown;
        }

        private void process(String path)
        {
            DetailBox.Text = "";
            String[] status = { "", "" };

            int counter = 0;
            String[] files = getFileNameInDirectory(path);
            pgb_top.Maximum = files.Length;
            foreach (var file in files)
            {
                FileInfo fa = new FileInfo(file);
                var fileCreationTime = fa.LastWriteTime.ToString();
                DateTime time = DateTime.ParseExact(fileCreationTime, "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);

                String dateTimePath = dateFolderGenerate(time);

                //String newPath = generate_folder(path, dateTimePath);
                status = move2folder(file, path, dateTimePath);
                

                DetailBox.Text += status[0] + "\r\n";
                counter += 1;
                TextStatisticBox.Text = "couter=" + counter.ToString();
                pgb_top.Value = counter;               


            }
            String logText = DetailBox.Text;
            writeLogFile(path, logText);
        }

        private void writeLogFile(String path, String text)
        {
            String fn = "log_file_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            File.WriteAllText(path+"/"+fn, text);
        }

        private String[] getFileNameInDirectory(String path)
        {
            String[] Files = Directory.GetFiles(path);
            return Files;
        }

        private String generate_folder(String mainPath, String name)
        {
            String path = System.IO.Path.Combine(mainPath, name);
            Directory.CreateDirectory(path);
            return path;

        } 

        private String[] move2folder(String srcPath, String dirPath, String dateTimePath ,  int stack=0)
        {
            String status = "";
            String[] sttr_status = { status, "-1" };
            try
            {
                FileInfo fi = new FileInfo(srcPath);
                String newFileName = newCleanExtension(srcPath);
                String desPath = "";

                //Create Directory when is real Extension (new 61/07/06)
                if (!newFileName.Equals("-1"))
                {
                    desPath = generate_folder(dirPath, dateTimePath);
                }
 
                //Environment.Exit(0);
                String fileDstPath = desPath + "/" + newFileName;

                //Console.WriteLine( fi.LastWriteTime.ToString());
                String fid = fi.LastWriteTime.ToString();


                if (newFileName.Equals("-1"))
                {
                    status = String.Format("NI\t{1}\t{0}\t{2}", fi.Name, fi.Extension, fid);
                    sttr_status[0] = status;
                    sttr_status[1] = "OK";
                    return sttr_status;
                }
                else
                {                    
                    if (!File.Exists(fileDstPath))
                    {
                        File.Move(srcPath, fileDstPath);
                        status = String.Format("MV\t{1}\t{0}\t{2}", newFileName, fi.Extension, fid);
                        sttr_status[0] = status;
                        sttr_status[1] = "OK";

                    }
                    else
                    {
                        status = String.Format("NM\t{1}\t{0}\t{2}", newFileName, fi.Extension, fid);
                        sttr_status[0] = status;
                        sttr_status[1] = "OK";
                    }

                }

            }
            catch (Exception e)
            {      
                // First problem when load image from internet :: File is Locked
                // Unlock Permission
                fileBasicSolution(srcPath);
                stack++;

                //Try agin
                if (stack < 5)
                {
                    status = move2folder(srcPath, dirPath, dateTimePath, stack)[0];
                    sttr_status[0] = status;
                    sttr_status[1] = "OK";
                }
                else
                {
                    status = String.Format("\nER\t---\t{0}\t{1}", srcPath, e.ToString());
                    sttr_status[0] = status;
                    sttr_status[1] = "ER";
                }
                
            }

            return sttr_status;
        }

        private String dateFolderGenerate(DateTime time)
        {
            String month = ((time.Month < 10) ? "0" : "")+time.Month.ToString();
            String year = ((time.Year < 2500) ? ((time.Year) + 543).ToString() : time.Year.ToString());
            String realPath = year + "/" + month;
            return realPath;
        }

        private void fileBasicSolution(String p)
        {
            FileAttributes attributes = File.GetAttributes(p);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // Make the file RW
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                File.SetAttributes(p, attributes);
                //Console.WriteLine("The {0} file is no longer RO.", p);
            }            

        }

        public byte[] FileToByteArray(string fileName)
        {
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);
            fs.Close();
            return buff;
        }

        private Boolean isAllowExtention(String extension)
        {
            String[] allowextension = { "jpg", "png", "gif"};            
            return Array.Exists(allowextension, ext => ext == extension);
        }

        private String newCleanExtension(String filePath)
        {
            /*
             *  This Function Should be only check File Extension and suggests new filename will be
             *  Do not Move or Manage File
             *  @input : String(Full_File_path)
             *  @output : String(New_file_name)
             */
            bool isImage = false;

            FileInfo file = new FileInfo(filePath);
            String fileName = file.Name;
            
            // Add to RealImageExtension Check
            Byte[] fileByte = FileToByteArray(filePath);
            ImageFormat fm = GetImageFormat(fileByte);

            String fileRealExtension = fm.ToString();

            //Console.WriteLine(fileRealExtension + "----" + file.Extension);

            // Check isImages
            if (isAllowExtention(fileRealExtension))
            {
                // Do anything when input filename is Images
                isImage = true;

                // Check And remove old Extension
                if (file.Extension != "-1")
                {
                    fileName = newCleanExtension_removeOldExtension(fileName, file.Extension);
                }

                // Check and remove "large" from filename
                if ( newCleanExtension_isLargeName(fileName) )
                {                    
                    fileName = newCleanExtension_largeNameClean(fileName);
                }
                
                fileName = fileName.Trim();
                fileName = fileName + "." + fileRealExtension;

            }

            return (isImage) ? fileName : "-1";
        }


        private String newCleanExtension_removeOldExtension(String filename, String extension)
        {
            return filename.Substring(0, filename.Length - extension.Length);
        }

        private bool newCleanExtension_isLargeName(String filename)
        {            
            return filename.EndsWith(" large");
        }

        private String newCleanExtension_largeNameClean(String filename)
        {
            //Console.WriteLine("Do GetLArgeeee");
            String tempFileName = filename;
            tempFileName = tempFileName.Substring(0, tempFileName.Length - 6);
            return tempFileName;
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        private void do_process(object sender, RoutedEventArgs e)
        {
            String path = TextPathBox.Text;
            if (System.Windows.MessageBox.Show("Do You want to Clean\n"+ path, "Clean?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    process(path);
                }
                catch (Exception edda)
                {

                    if (System.Windows.MessageBox.Show(edda + "\n" + path, "Program is Catch", MessageBoxButton.OK, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        Environment.Exit(0);
                    }
                }
                
            }
            
        }
        
    }
}

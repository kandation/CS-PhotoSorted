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

        private void process(String path)
        {
            DetailBox.Text = "";

            int counter = 0;
            foreach (var file in getFileNameInDirectory(path))
            {
                FileInfo fa = new FileInfo(file);
                var fileCreationTime = fa.LastWriteTime.ToString();
                DateTime time = DateTime.ParseExact(fileCreationTime, "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);

                String dateTimePath = dateFolderGenerate(time);

                String newPath = generate_folder(path, dateTimePath);
                String status = move2folder(file, newPath);

                DetailBox.Text += status + "\r\n";
                counter += 1;

                
            }
            TextStatisticBox.Text = "couter="+counter.ToString();
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

        private String move2folder(String srcPath, String desPath, int stack=0)
        {
            String status = "";
            try
            {
                FileInfo fi = new FileInfo(srcPath);
                String newFileName = cleanExtension(srcPath);
                String fileDstPath = desPath + "/" + newFileName;

                Console.WriteLine( fi.LastWriteTime.ToString());
                String fid = fi.LastWriteTime.ToString();


                if (newFileName.Equals("-1"))
                {
                    status = String.Format("NI\t{1}\t{0}\t{2}", fi.Name, fi.Extension, fid);
                    return status;
                }
                else
                {                    
                    if (!File.Exists(fileDstPath))
                    {
                        File.Move(srcPath, fileDstPath);
                        status = String.Format("MV\t{1}\t{0}\t{2}", newFileName, fi.Extension, fid);
                    }
                    else
                    {
                        status = String.Format("NM\t{1}\t{0}\t{2}", newFileName, fi.Extension, fid);
                    }

                }

            }
            catch (Exception e)
            {                
                fileBasicSolution(srcPath);
                stack++;
                if (stack < 5)
                {
                    status = move2folder(srcPath, desPath, stack);
                }
                else
                {
                    status = String.Format("\nER\t---\t{0}\t{1}", srcPath, e.ToString());
                }
            }

            return status;
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
                Console.WriteLine("The {0} file is no longer RO.", p);
            }
            
        }

        private String cleanExtension(String filePath)
        {
            
            String[] allowExtension = {".jpg", ".png", ".gif" };
            FileInfo file = new FileInfo(filePath);
            String newFileName = file.Name;
            String fn = file.Extension.ToLower();
            bool isImage = false;
            foreach (String al in allowExtension)
            {
                if (fn.Equals(al))
                {
                    isImage = true;
                }
                else
                {
                    int k = fn.IndexOf("large");
                    if (k > 0)
                    {
                        newFileName = file.Name.Substring(0, file.Name.Length - 6);
                        isImage = (!cleanExtension(file.Directory.ToString() + "/" + newFileName).Equals("-1"));
                        break;
                    }
                }
                
            }


            return (isImage) ? newFileName : "-1";
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
                process(path);
            }
            
        }
    }
}

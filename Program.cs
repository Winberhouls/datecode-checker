using System;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

class Program
{
    static void Main(string[] args)
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string ea3xmlPath = Path.Combine(currentDirectory, "prop", "ea3-config.xml");
        string dllPath = Path.Combine(currentDirectory, "modules", "bm2dx.dll");
        string sqlitePath = Path.Combine(currentDirectory, "file_sizes.db");
        Console.WriteLine("Current Directory: " + Directory.GetCurrentDirectory());

        // Check if files exist in the current directory, otherwise prompt the user to select a folder
        if (!File.Exists(ea3xmlPath) || !File.Exists(dllPath) || !File.Exists(sqlitePath))
        {
            MessageBox.Show("Some required files are missing. Please put this program next to the necessary directories.", "Missing Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return; // Exit the program
        }

        // Load XML file with correct encoding
        string xmlContent = File.ReadAllText(ea3xmlPath, Encoding.GetEncoding("utf-8"));
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        // Get value of tag <ext>
        string varExt = xmlDoc.SelectSingleNode("//soft/ext").InnerText;

        // Output 
        Console.WriteLine("XML datecode: " + varExt);

        // Read the XML file and extract the datecode
        string dateCode = varExt;

        // Calculate the size of the DLL file
        long dllSize = new FileInfo(dllPath).Length;

        // Connect to the SQLite database and retrieve the corresponding datecode
        string connectionString = $"Data Source={sqlitePath};Version=3;";
        string query = $"SELECT datecode FROM file_sizes WHERE FileSizeInBytes = {dllSize};";

        string dllDateCode = null;
        bool matched = false;

        try
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        dllDateCode = result.ToString();
                        matched = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error connecting to the SQLite database: " + ex.Message);
            matched = false;
        }
        // -------------------- File comparison -------------------------
        // Compare datecodes
        if (matched)
        {
            if (dllDateCode == dateCode)
            {
                Console.WriteLine($"Datecodes match!!");
            }
            else
            {
                Console.WriteLine($"Datecodes do not match. :v.");
                Console.Write("Do you want to fix it? (yes/no): ");
                string fix = Console.ReadLine();
                if (fix.ToLower() == "yes")
                {
                    Console.WriteLine("Fixing datecode in XML file.");
                    xmlDoc.SelectSingleNode("//soft/ext").InnerText = dllDateCode;
                    xmlDoc.Save(ea3xmlPath);
                    Console.WriteLine($"Datecode in XML file updated, now its {xmlDoc}");
                }
                else
                {
                    Console.WriteLine("All Right Then, Keep Your Secrets.");
                }
            }
        }
        else
        {
            Console.WriteLine($"No matching entry found in the SQLite database for DLL size: {dllSize} bytes.");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

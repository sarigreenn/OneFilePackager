using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Formats.Asn1;
var createRspCommand = new Command("create-rsp", "Create a response file for bundle command");
var bundelCommand = new Command("bundle", "bundele code files to a single file");
var bundleOption = new Option<FileInfo>("--output","File path and name")
{
    Name = "output"
};
var languageOption = new Option<string>("--language", "List of programming languages to include in the bundle")
{
    Name= "language",
    IsRequired = true
};
var noteOption = new Option<bool>("--note", "Include source code as a comment in the bundle file");
var sortOption = new Option<string>("--sort", "Sort order for code files (name or type)")
{
    Name = "sort",
    IsRequired = false
};

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from source code")
{
    IsRequired = false
};

var authorOption = new Option<string>("--author", "Author name to include in the bundle file")
{
    Name= "author",
    IsRequired = false
};

bundelCommand.AddOption(bundleOption);
bundelCommand.AddOption(languageOption);
bundelCommand.AddOption(noteOption);
bundelCommand.AddOption(sortOption);
bundelCommand.AddOption(removeEmptyLinesOption);
bundelCommand.AddOption(authorOption);
bundleOption.AddAlias("-o");
languageOption.AddAlias("-l");
noteOption.AddAlias("-n");
sortOption.AddAlias("-s");
removeEmptyLinesOption.AddAlias("-r");
authorOption.AddAlias("-a");
noteOption.SetDefaultValue(false);
removeEmptyLinesOption.SetDefaultValue(false);
static string RemoveEmptyLines(string code)
{
    string[] lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    List<string> nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    return string.Join(Environment.NewLine, nonEmptyLines);
}
createRspCommand.SetHandler(() =>
{
    var responseFile = new FileInfo("responseFile.rsp");

    try
    {
        using (StreamWriter rspwriter = new StreamWriter(responseFile.FullName))
        {
            Console.WriteLine("Enter file output name");
            string output;
            do
            {
                output = Console.ReadLine();
            } while (String.IsNullOrEmpty(output));
            rspwriter.Write($"--output {output} ");
            Console.WriteLine("Enter programming language or to include every language enter all");
            string language1;
            do
            {
                language1 = Console.ReadLine();
            } while (String.IsNullOrEmpty(language1));
            rspwriter.Write($"--language {language1} ");
            Console.WriteLine("Include source code origin as a comment? (y/n)");
            var noteInput = Console.ReadLine();
            rspwriter.Write(noteInput.Trim().ToLower() == "y" ? "--note " : "" );
            Console.WriteLine("Enter the sort order for code (name/type)" );
            rspwriter.Write($"--sort {Console.ReadLine()} " );
            Console.WriteLine("Remove empty lines from code (y/n)" );
            var RemoveEmptyLinesAns = Console.ReadLine();
            rspwriter.Write(RemoveEmptyLinesAns.Trim().ToLower() == "y" ? "--remove-empty-lines " : "" );
            Console.WriteLine("Enter the Author's name" );
            rspwriter.Write($"--author {Console.ReadLine()} ");
            Console.WriteLine($"Response file created successfully: {responseFile.FullName}");

        }


    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating response file: {ex.Message}");
    }
}
);
{

}
bundelCommand.SetHandler((output, language,note, sort, author) =>
{
    try
    {
        DirectoryInfo directory = new DirectoryInfo(".");
        FileInfo[] files;
        List<string> excludedDirectories = new List<string> { "bin", "debug" };
        files = directory.GetFiles()
            .Where(file => !excludedDirectories.Any(dir => file.FullName.ToLower().Contains(dir)))
            .ToArray();
        if (language.ToLower() == "all")
        {
            files = directory.GetFiles();
        }
        else
        {
            files = directory.GetFiles("*." + language);
        }
        if (!string.IsNullOrEmpty(sort))
        {
            switch (sort.ToLower())
            {
                case "name":
                    files = files.OrderBy(file => file.Name).ToArray();
                    break;
                case "type":
                    files = files.OrderBy(file => Path.GetExtension(file.Name)).ToArray();
                    break;
                default:
                    Console.WriteLine($"Error,invalid sort option '{sort}'. Defaulting to 'name'.");
                    files = files.OrderBy(file => file.Name).ToArray();
                    break;
            }
            Console.WriteLine("Sorted files:");
            foreach (var file in files)
            {
                Console.WriteLine(file.FullName);
            }
        }
        string outputPath = output.FullName;
        if(!Path.IsPathRooted(outputPath))
        {
            outputPath = Path.Combine(Environment.CurrentDirectory, outputPath);
        }
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Error: Directory {outputDirectory} does not exist.");
            return;
        }
        Console.WriteLine($"output Path: {outputPath}");
        Console.WriteLine($"num of files bundled : {files.Length}");
        using (StreamWriter writer=File.AppendText( outputPath ))
        {
            if (files.Length == 0)
            {
                Console.WriteLine("Error,No files were find to bundle");
                return;
            }
            foreach (var file in files)
            {
                string code=File.ReadAllText(file.FullName);
                if (!string.IsNullOrEmpty(author))
                {
                    writer.WriteLine($"//Author: {author}");
                }
                if(bundelCommand.Parse(args).HasOption(removeEmptyLinesOption))
                {
                    code=RemoveEmptyLines(code);
                }
                if (note)
                {
                    writer.WriteLine($"Source code from: {file.FullName}");
                }
                writer.WriteLine($"//file: {file.Name}");
                writer.WriteLine(code);
               
            }
            Console.WriteLine("file was created");
        }
      
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error, file path is invalid");
    }
}, bundleOption, languageOption,noteOption, sortOption, authorOption);

var rootCommand = new RootCommand("root command for file bundle CLI");
rootCommand.AddCommand(bundelCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);



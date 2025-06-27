using System.Diagnostics;
using System.Text;


Console.InputEncoding = System.Text.Encoding.Unicode;
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.Write("請輸入專案完整路徑(ex: D:\\A_project): ");
string repoPath = Console.ReadLine()?.Trim('"') ?? "";
if (!Directory.Exists(Path.Combine(repoPath, ".git")))
{
    Console.WriteLine("這不是一個有效的專案路徑");
    return;
}

Console.WriteLine("你要列出幾個 commit?（預設 10）: ");
string inputCount = Console.ReadLine()?.Trim() ?? "10";

// 顯示 git log
string log = RunGit(repoPath, "log --oneline");
var lines = log.Split('\n')
               .Where(l => !string.IsNullOrWhiteSpace(l))
               .Take(int.TryParse(inputCount, out int count) ? count : 10)
               .ToList();

Console.WriteLine("\n=== Commit 列表 ===");
for (int i = 0; i < lines.Count; i++)
    Console.WriteLine($"編號: {i} ; {lines[i]}");

Console.Write("\n輸入較舊的 commit 編號 (數字): ");
int.TryParse(Console.ReadLine()?.Trim() ?? "0", out int idx1);
Console.Write("輸入較新的 commit 編號 (數字): : ");
int.TryParse(Console.ReadLine()?.Trim() ?? "0", out int idx2);

if (idx1 < 0 || idx2 < 0 || idx1 >= lines.Count || idx2 >= lines.Count || idx1 == idx2)
{
    Console.WriteLine("無效的選擇");
    return;
}

string commit1 = lines[idx1].Split(' ')[0];
string commit2 = lines[idx2].Split(' ')[0];

Console.WriteLine($"\n比較 {commit1} ↔ {commit2} 差異檔案...");

// 執行 git diff
string diffOutput = RunGit(repoPath, $"diff --name-only {commit1} {commit2}");
var changedFiles = diffOutput.Split('\n')
                             .Where(f => !string.IsNullOrWhiteSpace(f))
                             .ToList();

if (!changedFiles.Any())
{
    Console.WriteLine("這兩個 commit 間沒有差異檔案");
    return;
}

Console.WriteLine($"\n共 {changedFiles.Count} 個檔案：");
foreach (var f in changedFiles)
    Console.WriteLine("  " + f);

// 選擇輸出資料夾
Console.Write("\n請輸入產出資料夾(絕對路徑D:\\XXX) 或一資料夾名(output_test): ");
string outputPath = Console.ReadLine()?.Trim('"') ?? "";
if (string.IsNullOrEmpty(outputPath))
{
    Console.WriteLine("未輸入輸出路徑");
    return;
}
if(!Path.IsPathRooted(outputPath))
{
    Console.WriteLine("非絕對路徑，將使用相對路徑放在程式目錄");
    outputPath = Path.Combine(Environment.CurrentDirectory, outputPath);
}

Directory.CreateDirectory(outputPath);

// 複製檔案
//Console.WriteLine($"目前目錄：{Environment.CurrentDirectory}");
Console.WriteLine($"\n開始複製檔案到: {outputPath}");
foreach (var file in changedFiles)
{
    string source = Path.Combine(repoPath, file);
    string target = Path.Combine(outputPath, file);
    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, true);
        Console.WriteLine($"複製: {file}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"錯誤複製 {file}: {ex.Message}");
    }
}

Console.WriteLine("\n✅ 完成");
Console.ReadKey();


static string RunGit(string repoPath, string args)
{
    var psi = new ProcessStartInfo("git", args)
    {
        WorkingDirectory = repoPath,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var process = Process.Start(psi)!;
    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
        throw new Exception("Git 錯誤: " + error);

    return output;
}
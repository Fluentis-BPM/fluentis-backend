/// <summary>
/// A professional commit message linter CSX script based on the Conventional Commits spec.
/// Supports optional breaking change markers (!).
/// For more info: https://www.conventionalcommits.org/en/v1.0.0/
/// </summary>
using System;
using System.IO;
using System.Text.RegularExpressions;

private var msg = File.ReadAllLines(Args[0])[0];

string commitFilePath = Args[0];
if (!File.Exists(commitFilePath))
{
   Console.ForegroundColor = ConsoleColor.Red;
   Console.WriteLine($"Error: Commit message file not found: {commitFilePath}");
   Environment.Exit(1);
}

// Read and trim the commit message.
string commitMessage = File.ReadAllText(commitFilePath).Trim();
if (string.IsNullOrEmpty(commitMessage))
{
   Console.ForegroundColor = ConsoleColor.Red;
   Console.WriteLine("Error: Commit message is empty.");
   Environment.Exit(1);
}

// Updated regex to support optional scope and breaking change marker (!)
// Explanation:
// - (?=.{10,100}$) ensures the entire message length is between 10 and 100 characters (adjust as needed).
// - Allowed types are enforced.
// - Optional scope is allowed: (optional parentheses with any non-')' chars).
// - An optional exclamation mark may appear either after the type or after the scope.
// - A colon and a space separate the header from the subject.
// - The subject must have at least one character.
string pattern = @"^(?=.{10,100}$)(?:(?:build|feat|ci|chore|docs|fix|perf|refactor|revert|style|test)(?:\([^\)]+\))?(!)?):\s.+$";

if (Regex.IsMatch(commitMessage, pattern))
{
   Environment.Exit(0);
}
else
{
   Console.ForegroundColor = ConsoleColor.Red;
   Console.WriteLine("Invalid commit message.");
   Console.ResetColor();
   Console.WriteLine("Commit message must follow the Conventional Commits format.");
   Console.WriteLine("Examples:");
   Console.WriteLine("  feat: add new login feature");
   Console.WriteLine("  fix(auth): correct password validation");
   Console.WriteLine("  feat(api)!: introduce breaking API changes");
   Console.WriteLine("More info: https://www.conventionalcommits.org/en/v1.0.0/");
   Environment.Exit(1);
}

using System.Reflection;

// Ensure only one AssemblyCompany attribute exists in the entire assembly.
// Remove any duplicate AssemblyCompany attribute declarations from all other files in the project.
// Keep this as the single source of truth for the company attribute.
[assembly: AssemblyCompany("Lindsay Inc ")]
[assembly: AssemblyProduct("CPCRemote.UI")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0.0")]
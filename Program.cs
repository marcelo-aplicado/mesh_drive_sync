namespace MeshDriveSync;
internal static class Program{
 [STAThread]static void Main(string[] args){ApplicationConfiguration.Initialize();using var mutex=new Mutex(true,"MeshDriveSync-v3-single-instance",out var first);if(!first)return;Application.Run(new MainForm(args.Contains("--background")));}
}

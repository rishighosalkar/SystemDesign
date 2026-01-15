namespace CompositeDesignPattern.FileSystem
{
    public class DirectorySystem : IFileSystem
    {
        List<IFileSystem> _fileSystem;
        string dirName;
        public DirectorySystem(string name)
        {
            dirName = name;
            _fileSystem = new List<IFileSystem>();
        }

        public void ls()
        {
            Console.WriteLine("Directory: " + dirName);
            foreach(var file in _fileSystem)
            {
                file.ls();
            }
        }

        public void AddFileOrDir(IFileSystem fileSystem)
        {
            _fileSystem.Add(fileSystem);
        }
    }
}

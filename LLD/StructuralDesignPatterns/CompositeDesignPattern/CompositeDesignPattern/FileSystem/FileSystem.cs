namespace CompositeDesignPattern.FileSystem
{
    public class FileSystem : IFileSystem
    {
        string fileName;
        public FileSystem(string name)
        {
            fileName = name;
        }
        public void AddFileOrDir(IFileSystem fs)
        {
            throw new NotImplementedException();
        }

        public void ls()
        {
            Console.WriteLine("File: " + fileName);
        }
    }
}

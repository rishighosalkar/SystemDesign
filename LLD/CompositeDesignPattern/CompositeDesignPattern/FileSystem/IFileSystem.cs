namespace CompositeDesignPattern.FileSystem
{
    public interface IFileSystem
    {
        public void ls();
        public void AddFileOrDir(IFileSystem fs);
    }
}

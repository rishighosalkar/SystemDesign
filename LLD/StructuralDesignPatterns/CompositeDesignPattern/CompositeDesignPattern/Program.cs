
using CompositeDesignPattern.FileSystem;

IFileSystem movieDir = new DirectorySystem("Movies");

IFileSystem inception = new FileSystem("Inception");

movieDir.AddFileOrDir(inception);

IFileSystem comdeyMovieDir = new DirectorySystem("Comedy Movies");
IFileSystem avengers = new FileSystem("Avengers");
comdeyMovieDir.AddFileOrDir(avengers);

movieDir.AddFileOrDir(comdeyMovieDir);

movieDir.ls();
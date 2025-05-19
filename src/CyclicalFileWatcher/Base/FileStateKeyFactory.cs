using System.Threading.Tasks;

namespace FileWatcher;

public delegate Task<string> FileStateKeyFactory<in TFileStateContent>(string filePath, TFileStateContent fileStateContent) where TFileStateContent : IFileStateContent;
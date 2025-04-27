using System.Threading.Tasks;

namespace FileWatcher.Base;

public delegate Task<string> FileStateKeyFactory<in TFileStateContent>(string filePath, TFileStateContent fileStateContent) where TFileStateContent : IFileStateContent;
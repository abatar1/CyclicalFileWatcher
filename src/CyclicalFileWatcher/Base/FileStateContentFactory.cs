using System.Threading.Tasks;

namespace FileWatcher.Base;

public delegate Task<TFileStateContent> FileStateContentFactory<TFileStateContent>(string filePath) where TFileStateContent : IFileStateContent;
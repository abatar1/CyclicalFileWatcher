using System.Threading.Tasks;

namespace FileWatcher;

public delegate Task<TFileStateContent> FileStateContentFactory<TFileStateContent>(string filePath) where TFileStateContent : IFileStateContent;
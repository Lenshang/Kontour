using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.Json;
using System.Globalization;
using KontourApp.Models;
using KontourApp.Services;

namespace KontourApp.ViewModels;

public class FileExplorerViewModel : INotifyPropertyChanged
{
    private readonly FileExplorerService _fileService;
    private readonly LocalizationService _localization;
    private string _currentPath = string.Empty;
    private DriveInfoModel? _selectedDrive;
    private FileItemModel? _selectedFile;
    private string _statusMessage = "就绪";
    private List<FileItemModel> _allFileItems = new();
    private DirectoryTreeNode? _selectedTreeNode;
    private bool _isPlaying;
    private double _currentPosition;
    private double _duration;
    private double _volume = 0.5;
    private string _currentAudioFile = string.Empty;
    private bool _showFolders = false;
    private LanguageModel? _selectedLanguage;

    public ObservableCollection<DriveInfoModel> Drives { get; }
    public ObservableCollection<FileItemModel> FileItems { get; }
    public ObservableCollection<string> PathHistory { get; }
    public ObservableCollection<FileExtensionFilterModel> ExtensionFilters { get; }
    public ObservableCollection<DirectoryTreeNode> DirectoryTree { get; }
    public ObservableCollection<FavoriteItemModel> Favorites { get; }
    public ObservableCollection<LanguageModel> AvailableLanguages { get; }

    public FileExplorerViewModel()
    {
        _fileService = new FileExplorerService();
        _localization = LocalizationService.Instance;
        Drives = new ObservableCollection<DriveInfoModel>();
        FileItems = new ObservableCollection<FileItemModel>();
        PathHistory = new ObservableCollection<string>();
        DirectoryTree = new ObservableCollection<DirectoryTreeNode>();
        Favorites = new ObservableCollection<FavoriteItemModel>();
        AvailableLanguages = new ObservableCollection<LanguageModel>
        {
            new LanguageModel { Code = "zh-CN", DisplayName = "简体中文", NativeName = "简体中文" },
            new LanguageModel { Code = "zh-TW", DisplayName = "繁體中文", NativeName = "繁體中文" },
            new LanguageModel { Code = "en", DisplayName = "English", NativeName = "English" },
            new LanguageModel { Code = "ja", DisplayName = "日本語", NativeName = "日本語" },
            new LanguageModel { Code = "ko", DisplayName = "한국어", NativeName = "한국어" }
        };
        
        // 初始化扩展名过滤器
        ExtensionFilters = new ObservableCollection<FileExtensionFilterModel>
        {
            new FileExtensionFilterModel { Extension = ".wav", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".mp3", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".ogg", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".fxp", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".nki", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".nksn", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".nkm", IsEnabled = true },
            new FileExtensionFilterModel { Extension = ".mid", IsEnabled = true }
        };
        
        // 订阅过滤器变化
        foreach (var filter in ExtensionFilters)
        {
            filter.PropertyChanged += (s, e) => ApplyFileFilter();
        }

        NavigateToPathCommand = new Command<string>(NavigateToPath);
        NavigateBackCommand = new Command(NavigateBack, () => PathHistory.Count > 1);
        NavigateUpCommand = new Command(NavigateUp, () => !string.IsNullOrEmpty(CurrentPath));
        RefreshCommand = new Command(Refresh);
        ItemClickedCommand = new Command<FileItemModel>(OnItemClicked);
        ItemDoubleClickedCommand = new Command<FileItemModel>(OnItemDoubleClicked);
        TreeNodeSelectedCommand = new Command<DirectoryTreeNode>(OnTreeNodeSelected);
        TreeNodeExpandedCommand = new Command<DirectoryTreeNode>(OnTreeNodeExpanded);
        PlayPauseCommand = new Command(TogglePlayPause);
        AddToFavoritesCommand = new Command<DirectoryTreeNode>(AddToFavorites);
        RemoveFromFavoritesCommand = new Command<FavoriteItemModel>(RemoveFromFavorites);
        FavoriteClickedCommand = new Command<FavoriteItemModel>(OnFavoriteClicked);
        
        // 订阅语言变化事件
        _localization.LanguageChanged += OnLanguageChanged;

        LoadDrives();
        LoadFavorites();
        LoadSettings();
    }

    public string CurrentPath
    {
        get => _currentPath;
        set
        {
            if (_currentPath != value)
            {
                _currentPath = value;
                OnPropertyChanged();
                ((Command)NavigateUpCommand).ChangeCanExecute();
            }
        }
    }

    public DriveInfoModel? SelectedDrive
    {
        get => _selectedDrive;
        set
        {
            if (_selectedDrive != value)
            {
                _selectedDrive = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    NavigateToPath(value.Name);
                }
            }
        }
    }

    public FileItemModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            System.Diagnostics.Debug.WriteLine($"SelectedFile setter 被调用, 新值: {value?.Name ?? "null"}");
            
            if (_selectedFile != value)
            {
                // 取消之前选中项的高亮
                if (_selectedFile != null)
                {
                    _selectedFile.IsSelected = false;
                    System.Diagnostics.Debug.WriteLine($"取消高亮: {_selectedFile.Name}");
                }
                
                _selectedFile = value;
                
                // 高亮新选中项
                if (_selectedFile != null)
                {
                    _selectedFile.IsSelected = true;
                    System.Diagnostics.Debug.WriteLine($"高亮文件: {_selectedFile.Name}, IsSelected={_selectedFile.IsSelected}");
                    
                    // 如果是音频文件，自动播放
                    if (!_selectedFile.IsDirectory)
                    {
                        var ext = _selectedFile.Extension.ToLowerInvariant();
                        System.Diagnostics.Debug.WriteLine($"文件扩展名: {ext}");
                        
                        if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
                        {
                            System.Diagnostics.Debug.WriteLine($"检测到音频文件，开始播放: {_selectedFile.FullPath}");
                            PlayAudioFile(_selectedFile.FullPath);
                        }
                        else if (ext == ".nki" || ext == ".nksn" || ext == ".fxp" || ext == ".nkm")
                        {
                            System.Diagnostics.Debug.WriteLine($"检测到 {ext} 文件，查找预览音频文件");
                            var previewFile = FindPreviewFile(_selectedFile.FullPath);
                            if (!string.IsNullOrEmpty(previewFile))
                            {
                                System.Diagnostics.Debug.WriteLine($"找到预览文件: {previewFile}");
                                PlayAudioFile(previewFile);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"未找到预览音频文件");
                                StatusMessage = string.Format(_localization["NoPreviewAudio"], Path.GetFileName(_selectedFile.FullPath));
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"非音频文件，跳过播放");
                        }
                    }
                }
                
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
            }
        }
    }

    public double CurrentPosition
    {
        get => _currentPosition;
        set
        {
            if (Math.Abs(_currentPosition - value) > 0.01)
            {
                _currentPosition = value;
                OnPropertyChanged();
            }
        }
    }

    public double Duration
    {
        get => _duration;
        set
        {
            if (Math.Abs(_duration - value) > 0.01)
            {
                _duration = value;
                OnPropertyChanged();
            }
        }
    }

    public double Volume
    {
        get => _volume;
        set
        {
            if (Math.Abs(_volume - value) > 0.01)
            {
                _volume = Math.Clamp(value, 0, 1);
                OnPropertyChanged();
                OnVolumeChanged?.Invoke(_volume);
            }
        }
    }

    public string CurrentAudioFile
    {
        get => _currentAudioFile;
        set
        {
            if (_currentAudioFile != value)
            {
                _currentAudioFile = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowFolders
    {
        get => _showFolders;
        set
        {
            if (_showFolders != value)
            {
                _showFolders = value;
                OnPropertyChanged();
                ApplyFileFilter();
                SaveSettings();
            }
        }
    }
    
    public LanguageModel? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value && value != null)
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                ChangeLanguage(value.Code);
            }
        }
    }
    
    public LocalizationService Localization => _localization;

    public ICommand NavigateToPathCommand { get; }
    public ICommand NavigateBackCommand { get; }
    public ICommand NavigateUpCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ItemClickedCommand { get; }
    public ICommand ItemDoubleClickedCommand { get; }
    public ICommand TreeNodeSelectedCommand { get; }
    public ICommand TreeNodeExpandedCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand AddToFavoritesCommand { get; }
    public ICommand RemoveFromFavoritesCommand { get; }
    public ICommand FavoriteClickedCommand { get; }

    // 音频播放事件
    public event Action<string>? OnPlayAudio;
    public event Action? OnPauseAudio;
    public event Action? OnStopAudio;
    public event Action<double>? OnVolumeChanged;
    public event Action<double>? OnSeekTo;
    
    // 树形导航事件
    public event Action<DirectoryTreeNode>? OnScrollToNode;

    private void LoadDrives()
    {
        Drives.Clear();
        var drives = _fileService.GetAvailableDrives();
        
        foreach (var drive in drives)
        {
            Drives.Add(drive);
        }

        StatusMessage = string.Format(_localization["DrivesFound"], drives.Count);
        
        // 初始化目录树，显示所有驱动器
        LoadAllDrivesAsRootNodes();
    }

    private void NavigateToPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (!_fileService.CanAccessPath(path))
            {
                StatusMessage = string.Format(_localization["CannotAccessPath"], path);
                return;
            }

            _allFileItems = _fileService.GetDirectoryContents(path);
            ApplyFileFilter();

            CurrentPath = path;
            
            if (PathHistory.Count == 0 || PathHistory[^1] != path)
            {
                PathHistory.Add(path);
            }

            UpdateStatusMessage();
            ((Command)NavigateBackCommand).ChangeCanExecute();
            
            // 更新树形结构
            UpdateDirectoryTree();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(_localization["Error"], ex.Message);
        }
    }

    private void ApplyFileFilter()
    {
        FileItems.Clear();
        
        var enabledExtensions = ExtensionFilters
            .Where(f => f.IsEnabled)
            .Select(f => f.Extension.ToLowerInvariant())
            .ToHashSet();
        
        foreach (var item in _allFileItems)
        {
            // 根据ShowFolders决定是否显示文件夹
            if (item.IsDirectory)
            {
                if (ShowFolders)
                {
                    FileItems.Add(item);
                }
            }
            else if (enabledExtensions.Count > 0)
            {
                var ext = item.Extension.ToLowerInvariant();
                if (enabledExtensions.Contains(ext))
                {
                    FileItems.Add(item);
                }
            }
        }
        
        UpdateStatusMessage();
    }
    
    private void UpdateStatusMessage()
    {
        var totalFiles = _allFileItems.Count(x => !x.IsDirectory);
        var totalDirs = _allFileItems.Count(x => x.IsDirectory);
        var visibleFiles = FileItems.Count(x => !x.IsDirectory);
        var visibleDirs = FileItems.Count(x => x.IsDirectory);
        
        if (visibleFiles < totalFiles)
        {
            StatusMessage = string.Format(_localization["FilesDisplay"], visibleFiles, totalFiles, visibleDirs);
        }
        else
        {
            StatusMessage = string.Format(_localization["FilesTotal"], totalFiles, totalDirs);
        }
    }
    
    private void UpdateDirectoryTree()
    {
        // 初始化时加载所有驱动器
        if (DirectoryTree.Count == 0)
        {
            LoadAllDrivesAsRootNodes();
        }
            
        // 更新树中的选中状态
        UpdateTreeSelection(CurrentPath);
    }
        
    private void UpdateTreeSelection(string currentPath)
    {
        // 递归查找并高亮当前路径
        foreach (var node in DirectoryTree)
        {
            UpdateNodeSelection(node, currentPath);
        }
    }
        
    private bool UpdateNodeSelection(DirectoryTreeNode node, string currentPath)
    {
        // 检查当前节点是否匹配
        if (node.FullPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
        {
            // 取消之前的选中
            if (_selectedTreeNode != null && _selectedTreeNode != node)
            {
                _selectedTreeNode.IsSelected = false;
            }
                
            node.IsSelected = true;
            _selectedTreeNode = node;
            return true;
        }
            
        // 检查子节点
        foreach (var child in node.Children)
        {
            if (UpdateNodeSelection(child, currentPath))
            {
                return true;
            }
        }
            
        // 如果不是当前路径，取消选中
        if (node.IsSelected && !node.FullPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
        {
            node.IsSelected = false;
        }
            
        return false;
    }
    
    private void LoadAllDrivesAsRootNodes()
    {
        try
        {
            DirectoryTree.Clear();
            
            foreach (var drive in Drives)
            {
                var driveNode = new DirectoryTreeNode
                {
                    Name = drive.DisplayName,
                    FullPath = drive.Name,
                    IsExpanded = false,
                    IsLoaded = false
                };
                
                // 添加占位符，表示有子目录
                driveNode.Children.Add(new DirectoryTreeNode { Name = "...", FullPath = "" });
                
                DirectoryTree.Add(driveNode);
            }
            
            System.Diagnostics.Debug.WriteLine($"已加载 {DirectoryTree.Count} 个驱动器节点");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载驱动器节点失败: {ex.Message}");
        }
    }
    
    private bool HasSubDirectories(string path)
    {
        try
        {
            return Directory.GetDirectories(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }
    
    private void OnTreeNodeExpanded(DirectoryTreeNode? node)
    {
        if (node == null || string.IsNullOrEmpty(node.FullPath)) return;
        
        // 切换展开状态
        node.IsExpanded = !node.IsExpanded;
        
        // 如果是展开且还未加载子节点，则加载
        if (node.IsExpanded && !node.IsLoaded)
        {
            LoadChildDirectories(node);
        }
    }
    
    private void LoadChildDirectories(DirectoryTreeNode node)
    {
        try
        {
            // 移除占位符
            node.Children.Clear();
            
            var directories = Directory.GetDirectories(node.FullPath);
            
            foreach (var dir in directories.Take(100))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    
                    // 跳过 .previews 文件夹
                    if (dirInfo.Name.Equals(".previews", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    var childNode = new DirectoryTreeNode
                    {
                        Name = dirInfo.Name,
                        FullPath = dirInfo.FullName
                    };
                    
                    // 检查子目录是否还有子目录
                    if (HasSubDirectories(dirInfo.FullName))
                    {
                        childNode.Children.Add(new DirectoryTreeNode { Name = "...", FullPath = "" });
                    }
                    
                    node.Children.Add(childNode);
                }
                catch
                {
                    // 忽略无权限访问的目录
                }
            }
            
            node.IsLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载子目录失败: {ex.Message}");
        }
    }
    
    private void OnTreeNodeSelected(DirectoryTreeNode? node)
    {
        if (node != null)
        {
            // 取消之前选中的节点
            if (_selectedTreeNode != null)
            {
                _selectedTreeNode.IsSelected = false;
            }
            
            // 设置新选中的节点
            _selectedTreeNode = node;
            node.IsSelected = true;
            
            NavigateToPath(node.FullPath);
        }
    }

    private void NavigateBack()
    {
        if (PathHistory.Count > 1)
        {
            PathHistory.RemoveAt(PathHistory.Count - 1);
            var previousPath = PathHistory[^1];
            CurrentPath = previousPath;
            NavigateToPath(previousPath);
        }
    }

    private void NavigateUp()
    {
        if (!string.IsNullOrEmpty(CurrentPath))
        {
            var parentPath = Directory.GetParent(CurrentPath)?.FullName;
            if (!string.IsNullOrEmpty(parentPath))
            {
                NavigateToPath(parentPath);
            }
        }
    }

    private void Refresh()
    {
        if (!string.IsNullOrEmpty(CurrentPath))
        {
            NavigateToPath(CurrentPath);
        }
        else
        {
            LoadDrives();
        }
    }

    private void OnItemClicked(FileItemModel? item)
    {
        System.Diagnostics.Debug.WriteLine($"========== OnItemClicked 被调用 ==========");
        
        if (item == null)
        {
            System.Diagnostics.Debug.WriteLine("项目为 null");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"点击项目: {item.Name}");
        
        // 设置选中项，这会触发 SelectedFile 的 setter
        SelectedFile = item;
    }

    private void OnItemDoubleClicked(FileItemModel? item)
    {
        System.Diagnostics.Debug.WriteLine($"========== OnItemDoubleClicked 被调用 ==========");
        
        if (item == null) return;

        if (item.IsDirectory)
        {
            System.Diagnostics.Debug.WriteLine($"双击文件夹，进入: {item.FullPath}");
            NavigateToPath(item.FullPath);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"双击文件: {item.Name}");
            StatusMessage = $"选中文件: {item.Name} ({item.DisplaySize})";
        }
    }

    private void PlayAudioFile(string filePath)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"PlayAudioFile 调用: {filePath}");
            CurrentAudioFile = filePath;
            
            System.Diagnostics.Debug.WriteLine($"OnPlayAudio 事件订阅者数量: {OnPlayAudio?.GetInvocationList().Length ?? 0}");
            OnPlayAudio?.Invoke(filePath);
            
            IsPlaying = true;
            System.Diagnostics.Debug.WriteLine($"IsPlaying 设置为 true");
            
            StatusMessage = string.Format(_localization["Playing"], Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"播放失败: {ex.Message}");
            StatusMessage = string.Format(_localization["Error"], ex.Message);
        }
    }

    private void TogglePlayPause()
    {
        if (IsPlaying)
        {
            OnPauseAudio?.Invoke();
            IsPlaying = false;
            StatusMessage = _localization["Paused"];
        }
        else if (!string.IsNullOrEmpty(CurrentAudioFile))
        {
            OnPlayAudio?.Invoke(CurrentAudioFile);
            IsPlaying = true;
            StatusMessage = string.Format(_localization["Playing"], Path.GetFileName(CurrentAudioFile));
        }
    }

    public void UpdatePlaybackPosition(double position, double duration)
    {
        CurrentPosition = position;
        Duration = duration;
    }

    public void SeekToPosition(double position)
    {
        OnSeekTo?.Invoke(position);
    }

    /// <summary>
    /// 查找 .nki/.nksn/.fxp 文件对应的预览音频文件
    /// 规则：在同级目录的 .previews 文件夹中查找与文件名相同但扩展名为 .ogg/.mp3/.wav 的文件
    /// </summary>
    private string? FindPreviewFile(string sourceFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(sourceFilePath);
            if (string.IsNullOrEmpty(directory))
                return null;

            var fileName = Path.GetFileName(sourceFilePath);
            var previewsDir = Path.Combine(directory, ".previews");
            
            System.Diagnostics.Debug.WriteLine($"查找预览文件夹: {previewsDir}");
            
            if (!Directory.Exists(previewsDir))
            {
                System.Diagnostics.Debug.WriteLine($"预览文件夹不存在: {previewsDir}");
                return null;
            }

            // 按优先级查找：.ogg -> .mp3 -> .wav
            string[] extensions = { ".ogg", ".mp3", ".wav" };
            
            foreach (var ext in extensions)
            {
                var previewFile = Path.Combine(previewsDir, fileName + ext);
                System.Diagnostics.Debug.WriteLine($"尝试查找: {previewFile}");
                
                if (File.Exists(previewFile))
                {
                    System.Diagnostics.Debug.WriteLine($"找到预览文件: {previewFile}");
                    return previewFile;
                }
            }
            
            System.Diagnostics.Debug.WriteLine("未找到任何预览文件");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"查找预览文件时出错: {ex.Message}");
            return null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 收藏夹管理
    private string FavoritesFilePath => Path.Combine(FileSystem.AppDataDirectory, "favorites.json");
    private string SettingsFilePath => Path.Combine(FileSystem.AppDataDirectory, "settings.json");

    private void AddToFavorites(DirectoryTreeNode? node)
    {
        if (node == null || string.IsNullOrEmpty(node.FullPath)) return;

        // 检查是否已存在
        if (Favorites.Any(f => f.FullPath.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = string.Format(_localization["AlreadyInFavorites"], node.Name);
            return;
        }

        var favorite = new FavoriteItemModel
        {
            Name = node.Name,
            FullPath = node.FullPath
        };

        Favorites.Add(favorite);
        SaveFavorites();
        StatusMessage = string.Format(_localization["AddedToFavorites"], node.Name);
    }

    private void RemoveFromFavorites(FavoriteItemModel? favorite)
    {
        if (favorite == null) return;

        Favorites.Remove(favorite);
        SaveFavorites();
        StatusMessage = string.Format(_localization["RemovedFromFavorites"], favorite.Name);
    }

    private void OnFavoriteClicked(FavoriteItemModel? favorite)
    {
        if (favorite == null) return;
        
        System.Diagnostics.Debug.WriteLine($"========== 收藏夹点击: {favorite.Name} ==========");
        System.Diagnostics.Debug.WriteLine($"目标路径: {favorite.FullPath}");
        
        StatusMessage = $"正在导航到收藏夹: {favorite.Name}...";

        if (Directory.Exists(favorite.FullPath))
        {
            System.Diagnostics.Debug.WriteLine("开始导航到路径...");
            
            // 先导航到目标路径，这会更新右侧文件列表
            NavigateToPath(favorite.FullPath);
            
            // 等待一下让UI更新，然后再展开左侧树
            System.Diagnostics.Debug.WriteLine("开始展开并滚动...");
            Task.Delay(50).ContinueWith(_ => 
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ExpandAndScrollToPath(favorite.FullPath);
                });
            });
        }
        else
        {
            StatusMessage = $"路径不存在: {favorite.FullPath}";
        }
    }

    private void LoadFavorites()
    {
        try
        {
            if (File.Exists(FavoritesFilePath))
            {
                var json = File.ReadAllText(FavoritesFilePath);
                var favorites = JsonSerializer.Deserialize<List<FavoriteItemModel>>(json);

                if (favorites != null)
                {
                    Favorites.Clear();
                    foreach (var favorite in favorites)
                    {
                        Favorites.Add(favorite);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载收藏夹失败: {ex.Message}");
        }
    }

    private void SaveFavorites()
    {
        try
        {
            var json = JsonSerializer.Serialize(Favorites.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(FavoritesFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存收藏夹失败: {ex.Message}");
        }
    }
    
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (settings != null)
                {
                    if (settings.ContainsKey("ShowFolders"))
                    {
                        var showFoldersValue = settings["ShowFolders"];
                        if (showFoldersValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
                        {
                            _showFolders = true;
                        }
                        else if (showFoldersValue is JsonElement jsonElement2 && jsonElement2.ValueKind == JsonValueKind.False)
                        {
                            _showFolders = false;
                        }
                        else if (showFoldersValue is bool boolValue)
                        {
                            _showFolders = boolValue;
                        }
                        
                        OnPropertyChanged(nameof(ShowFolders));
                        System.Diagnostics.Debug.WriteLine($"加载设置: ShowFolders = {_showFolders}");
                    }
                    
                    if (settings.ContainsKey("Language"))
                    {
                        var languageValue = settings["Language"];
                        string? languageCode = null;
                        
                        if (languageValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                        {
                            languageCode = jsonElement.GetString();
                        }
                        else if (languageValue is string strValue)
                        {
                            languageCode = strValue;
                        }
                        
                        if (!string.IsNullOrEmpty(languageCode))
                        {
                            try
                            {
                                var culture = new CultureInfo(languageCode);
                                _localization.CurrentCulture = culture;
                                
                                // 设置选中的语言
                                _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == languageCode);
                                OnPropertyChanged(nameof(SelectedLanguage));
                                
                                System.Diagnostics.Debug.WriteLine($"加载设置: Language = {languageCode}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"设置语言失败: {ex.Message}");
                            }
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("设置文件不存在，使用默认值");
                // 设置默认语言为简体中文
                _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == "zh-CN");
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
        }
    }
    
    private void SaveSettings()
    {
        try
        {
            // 读取现有设置以保留窗口大小等其他设置
            var settings = new Dictionary<string, object>();
            if (File.Exists(SettingsFilePath))
            {
                var existingJson = File.ReadAllText(SettingsFilePath);
                var existingSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);
                if (existingSettings != null)
                {
                    foreach (var kvp in existingSettings)
                    {
                        settings[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            // 更新当前修改的设置
            settings["ShowFolders"] = ShowFolders;
            settings["Language"] = _localization.CurrentCulture.Name;
            
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsFilePath, json);
            System.Diagnostics.Debug.WriteLine($"保存设置: ShowFolders = {ShowFolders}, Language = {_localization.CurrentCulture.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
        }
    }
    
    public void SaveWindowSettings(double width, double height, double treeWidth)
    {
        try
        {
            // 读取现有设置
            var settings = new Dictionary<string, object>();
            if (File.Exists(SettingsFilePath))
            {
                var existingJson = File.ReadAllText(SettingsFilePath);
                var existingSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);
                if (existingSettings != null)
                {
                    foreach (var kvp in existingSettings)
                    {
                        settings[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            // 更新窗口设置
            settings["WindowWidth"] = width;
            settings["WindowHeight"] = height;
            settings["TreeWidth"] = treeWidth;
            
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsFilePath, json);
            System.Diagnostics.Debug.WriteLine($"保存窗口设置: Width={width}, Height={height}, TreeWidth={treeWidth}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存窗口设置失败: {ex.Message}");
        }
    }
    
    public (double width, double height, double treeWidth) LoadWindowSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (settings != null)
                {
                    double width = 1200;  // 默认宽度
                    double height = 800;  // 默认高度
                    double treeWidth = 250;  // 默认树形宽度
                    
                    if (settings.ContainsKey("WindowWidth") && settings["WindowWidth"].ValueKind == JsonValueKind.Number)
                    {
                        width = settings["WindowWidth"].GetDouble();
                    }
                    
                    if (settings.ContainsKey("WindowHeight") && settings["WindowHeight"].ValueKind == JsonValueKind.Number)
                    {
                        height = settings["WindowHeight"].GetDouble();
                    }
                    
                    if (settings.ContainsKey("TreeWidth") && settings["TreeWidth"].ValueKind == JsonValueKind.Number)
                    {
                        treeWidth = settings["TreeWidth"].GetDouble();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"加载窗口设置: Width={width}, Height={height}, TreeWidth={treeWidth}");
                    return (width, height, treeWidth);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载窗口设置失败: {ex.Message}");
        }
        
        // 返回默认值
        return (1200, 800, 250);
    }
    
    private void ChangeLanguage(string languageCode)
    {
        try
        {
            var culture = new CultureInfo(languageCode);
            _localization.CurrentCulture = culture;
            SaveSettings();
            System.Diagnostics.Debug.WriteLine($"语言已切换到: {languageCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换语言失败: {ex.Message}");
        }
    }
    
    private void OnLanguageChanged()
    {
        // 通知界面更新所有本地化字符串
        OnPropertyChanged(nameof(Localization));
        System.Diagnostics.Debug.WriteLine("语言已更新，通知UI刷新");
    }
    
    // 键盘导航：选择下一个文件
    public void SelectNextFile()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SelectNextFile 被调用");
            
            if (FileItems.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("文件列表为空");
                return;
            }
            
            int currentIndex = -1;
            if (SelectedFile != null)
            {
                currentIndex = FileItems.IndexOf(SelectedFile);
                System.Diagnostics.Debug.WriteLine($"当前选中索引: {currentIndex}");
            }
            
            int nextIndex = currentIndex + 1;
            if (nextIndex >= FileItems.Count)
            {
                nextIndex = 0; // 循环到第一个
            }
            
            System.Diagnostics.Debug.WriteLine($"下一个索引: {nextIndex}");
            SelectedFile = FileItems[nextIndex];
            System.Diagnostics.Debug.WriteLine($"已选中: {SelectedFile.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SelectNextFile 失败: {ex.Message}");
        }
    }
    
    // 键盘导航：选择上一个文件
    public void SelectPreviousFile()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SelectPreviousFile 被调用");
            
            if (FileItems.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("文件列表为空");
                return;
            }
            
            int currentIndex = -1;
            if (SelectedFile != null)
            {
                currentIndex = FileItems.IndexOf(SelectedFile);
                System.Diagnostics.Debug.WriteLine($"当前选中索引: {currentIndex}");
            }
            
            int prevIndex = currentIndex - 1;
            if (prevIndex < 0)
            {
                prevIndex = FileItems.Count - 1; // 循环到最后一个
            }
            
            System.Diagnostics.Debug.WriteLine($"上一个索引: {prevIndex}");
            SelectedFile = FileItems[prevIndex];
            System.Diagnostics.Debug.WriteLine($"已选中: {SelectedFile.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SelectPreviousFile 失败: {ex.Message}");
        }
    }
    
    // 展开并滚动到指定路径
    private void ExpandAndScrollToPath(string targetPath)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"\n========== ExpandAndScrollToPath 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"目标路径: {targetPath}");
            
            StatusMessage = "正在展开目录树...";
            
            // 获取路径的各个部分
            var pathParts = GetPathParts(targetPath);
            System.Diagnostics.Debug.WriteLine($"路径分段数: {pathParts.Count}");
            
            StatusMessage = $"路径层级: {pathParts.Count} 层";
            
            foreach (var part in pathParts)
            {
                System.Diagnostics.Debug.WriteLine($"  - {part}");
            }
            
            if (pathParts.Count == 0) 
            {
                System.Diagnostics.Debug.WriteLine("警告: 路径分段为空");
                StatusMessage = "错误: 无法解析路径";
                return;
            }
            
            // 确保根目录已加载
            var rootPath = pathParts[0];
            System.Diagnostics.Debug.WriteLine($"根路径: {rootPath}");
            System.Diagnostics.Debug.WriteLine($"当前路径: {CurrentPath}");
            System.Diagnostics.Debug.WriteLine($"目录树节点数: {DirectoryTree.Count}");
            
            // 从根节点开始展开
            DirectoryTreeNode? currentNode = null;
            for (int i = 0; i < pathParts.Count; i++)
            {
                var currentPath = pathParts[i];
                System.Diagnostics.Debug.WriteLine($"\n处理第 {i + 1} 层: {currentPath}");
                
                StatusMessage = $"展开第 {i + 1}/{pathParts.Count} 层...";
                
                if (i == 0)
                {
                    // 查找根节点
                    currentNode = DirectoryTree.FirstOrDefault(n => 
                        n.FullPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase));
                    System.Diagnostics.Debug.WriteLine($"根节点查找结果: {(currentNode != null ? "找到" : "未找到")}");
                    
                    if (currentNode == null)
                    {
                        // 显示所有根节点用于调试
                        System.Diagnostics.Debug.WriteLine($"目录树中的根节点 ({DirectoryTree.Count} 个):");
                        foreach (var node in DirectoryTree.Take(10))
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {node.FullPath}");
                        }
                        StatusMessage = $"错误: 未找到根节点. 目录树有{DirectoryTree.Count}个节点。查找: {currentPath}";
                    }
                }
                else if (currentNode != null)
                {
                    System.Diagnostics.Debug.WriteLine($"当前节点: {currentNode.Name}");
                    System.Diagnostics.Debug.WriteLine($"当前节点完整路径: {currentNode.FullPath}");
                    System.Diagnostics.Debug.WriteLine($"当前节点展开状态: {currentNode.IsExpanded}");
                    System.Diagnostics.Debug.WriteLine($"当前节点加载状态: {currentNode.IsLoaded}");
                    System.Diagnostics.Debug.WriteLine($"子节点数: {currentNode.Children.Count}");
                    
                    // 展开当前节点（如果还未展开）
                    if (!currentNode.IsExpanded)
                    {
                        System.Diagnostics.Debug.WriteLine("展开节点...");
                        currentNode.IsExpanded = true;
                        if (!currentNode.IsLoaded)
                        {
                            System.Diagnostics.Debug.WriteLine("加载子目录...");
                            LoadChildDirectories(currentNode);
                            System.Diagnostics.Debug.WriteLine($"加载后子节点数: {currentNode.Children.Count}");
                        }
                    }
                    
                    // 在子节点中查找下一个节点
                    System.Diagnostics.Debug.WriteLine($"在 {currentNode.Children.Count} 个子节点中查找: {currentPath}");
                    foreach (var child in currentNode.Children.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"  子节点: {child.FullPath}");
                    }
                    if (currentNode.Children.Count > 5)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ... 还有 {currentNode.Children.Count - 5} 个子节点");
                    }
                    
                    var nextNode = currentNode.Children.FirstOrDefault(n => 
                        n.FullPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase));
                    System.Diagnostics.Debug.WriteLine($"子节点查找结果: {(nextNode != null ? $"找到 {nextNode.Name}" : "未找到")}");
                    currentNode = nextNode;
                }
                
                if (currentNode == null)
                {
                    System.Diagnostics.Debug.WriteLine($"错误: 无法找到节点: {currentPath}");
                    StatusMessage = string.Format(_localization["DirectoryNotFound"], i+1, Path.GetFileName(currentPath));
                    break;
                }
            }
            
            // 如果找到了目标节点，触发滚动事件
            if (currentNode != null && currentNode.FullPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"\n成功找到目标节点: {currentNode.Name}");
                
                StatusMessage = _localization["LocatingFolder"];
                
                // 确保节点被选中
                if (_selectedTreeNode != null)
                {
                    _selectedTreeNode.IsSelected = false;
                }
                currentNode.IsSelected = true;
                _selectedTreeNode = currentNode;
                
                // 触发滚动事件
                System.Diagnostics.Debug.WriteLine($"OnScrollToNode 事件订阅者数: {OnScrollToNode?.GetInvocationList().Length ?? 0}");
                if (OnScrollToNode != null)
                {
                    System.Diagnostics.Debug.WriteLine("触发 OnScrollToNode 事件");
                    OnScrollToNode.Invoke(currentNode);
                    StatusMessage = string.Format(_localization["LocatedTo"], currentNode.Name);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("警告: OnScrollToNode 事件无订阅者");
                    StatusMessage = _localization["ScrollEventNotSubscribed"];
                }
                
                System.Diagnostics.Debug.WriteLine($"已展开并滚动到: {targetPath}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"\n错误: 未找到目标节点或路径不匹配");
                if (currentNode != null)
                {
                    System.Diagnostics.Debug.WriteLine($"当前节点路径: {currentNode.FullPath}");
                    System.Diagnostics.Debug.WriteLine($"目标路径: {targetPath}");
                }
                StatusMessage = _localization["CannotLocateFolder"];
            }
            
            System.Diagnostics.Debug.WriteLine("========== ExpandAndScrollToPath 结束 ==========\n");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"展开路径失败: {ex.Message}\n{ex.StackTrace}");
            StatusMessage = $"错误: {ex.Message}";
        }
    }
    
    // 将完整路径拆分为各级目录路径
    private List<string> GetPathParts(string fullPath)
    {
        var parts = new List<string>();
        
        try
        {
            var dirInfo = new DirectoryInfo(fullPath);
            var current = dirInfo;
            
            while (current != null)
            {
                var path = current.FullName;
                // 确保根目录以反斜杠结尾（如 "C:\"）
                if (current.Parent == null && !path.EndsWith("\\"))
                {
                    path += "\\";
                }
                parts.Insert(0, path);
                System.Diagnostics.Debug.WriteLine($"路径段: {path}");
                current = current.Parent;
            }
            
            System.Diagnostics.Debug.WriteLine($"GetPathParts 返回 {parts.Count} 个路径段");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"解析路径失败: {ex.Message}");
        }
        
        return parts;
    }
}

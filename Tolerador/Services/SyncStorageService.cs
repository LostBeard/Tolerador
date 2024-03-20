using SpawnDev.BlazorJS.BrowserExtension.JSObjects;
using SpawnDev.BlazorJS.BrowserExtension.Services;

namespace Tolerador.Services
{
    public class SyncStorageService
    {
        StorageArea? SyncStorage { get; set; }
        BrowserExtensionService BrowserExtensionService;
        public SyncStorageService(BrowserExtensionService browserExtensionService)
        {
            BrowserExtensionService = browserExtensionService;
            SyncStorage = BrowserExtensionService.Chrome?.Storage?.Sync;
        }
    }
}

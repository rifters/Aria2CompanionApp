# Settings Configuration

## ?? Settings Location

Settings are stored in your **Windows user profile** to survive app updates and rebuilds.

**Location:**
```
%LOCALAPPDATA%\Aria2CompanionApp\settings.json
```

**Full path example:**
```
C:\Users\YourName\AppData\Local\Aria2CompanionApp\settings.json
```

**To view your settings:**
```powershell
notepad $env:LOCALAPPDATA\Aria2CompanionApp\settings.json
```

## ?? Configuration Options

### Via Settings UI (Recommended)

1. Run the app
2. Right-click the tray icon
3. Select **Settings...**
4. Configure your options
5. Click **Save**

### Manual Editing

You can also edit `settings.json` directly. See `settings.example.json` for template.

## ?? Settings Structure

```json
{
  "rpcSettings": {
    "rpcUrl": "http://192.168.1.100:6800/jsonrpc",
    "wsUrl": "ws://192.168.1.100:6800/jsonrpc",
    "token": "your-secret-token-here",
    "defaultDownloadDir": "/downloads"
  },
  "pathMappings": [
    {
      "linuxPrefix": "/share/downloads",
      "windowsPrefix": "X:\\Downloads",
      "description": "NAS Downloads"
    }
  ],
  "pollingIntervalMs": 3000,
  "version": "1.0"
}
```

## ??? Path Mappings Explained

**Problem:** Aria2 running on a NAS returns Linux paths like `/share/downloads/file.zip`, which don't exist on your Windows PC.

**Solution:** Path mappings translate these paths automatically.

### Example Scenarios

#### QNAP NAS
```json
{
  "linuxPrefix": "/share/CACHEDEV1_DATA/downloads",
  "windowsPrefix": "X:\\",
  "description": "QNAP mapped to X: drive"
}
```

#### Synology NAS
```json
{
  "linuxPrefix": "/volume1/downloads",
  "windowsPrefix": "\\\\192.168.1.100\\downloads",
  "description": "Synology UNC path"
}
```

#### Unraid
```json
{
  "linuxPrefix": "/mnt/user/downloads",
  "windowsPrefix": "Z:\\downloads",
  "description": "Unraid mapped drive"
}
```

### Testing Mappings

1. Open Settings ? Path Mappings
2. Select a mapping
3. Click **Test Mapping**
4. Verifies if the Windows path exists and is accessible

## ?? Security Notes

- `settings.json` is **gitignored** (never committed)
- Your RPC token and IP address stay private
- Safe to use on public repositories

## ?? Migration from Old Versions

If you're upgrading from a version with hardcoded settings:

1. First run will detect old settings
2. Offers one-time migration
3. Creates `settings.json` with your values
4. Old `Settings.cs` becomes defaults-only

## ?? Troubleshooting

### Settings not saving
- Check file permissions in app directory
- Make sure app isn't running as read-only

### Path mappings not working
- Use **Test Mapping** to verify Windows path exists
- Ensure network share is mounted
- Check if drive letter is correct

### Can't connect to Aria2
- Click **Test Connection** in Settings
- Verify Aria2 is running: `aria2c --enable-rpc`
- Check firewall isn't blocking connection
- Confirm RPC token matches your Aria2 config

## ?? First-Time Setup

1. **Start Aria2 on your NAS:**
   ```bash
   aria2c --enable-rpc --rpc-secret=YOUR_SECRET_TOKEN
   ```

2. **Configure App Settings:**
   - Right-click tray icon ? Settings
   - Enter your NAS IP and token
   - Add path mapping for downloads folder
   - Test connection
   - Save

3. **Verify:**
   - Add a test download
   - Check it appears in Downloads window
   - Try "Move file..." to test path mapping

## ?? Advanced Tips

- **Multiple mappings:** Add multiple for different folders
- **Longest match wins:** More specific prefixes take priority
- **UNC paths work:** Use `\\server\share` format
- **Test before save:** Use Test Connection/Mapping buttons

---

**Need help?** Open an issue on GitHub!

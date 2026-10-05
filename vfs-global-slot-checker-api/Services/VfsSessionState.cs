namespace vfs_global_slot_checker_api.Services;

public sealed class VfsSessionState
{
    private readonly Lock sync = new();
    private string? loginUser;
    private string? authorize;
    private string? clientSource;

    public void Set(string loginUser, string authorize, string clientSource)
    {
        lock (sync)
        {
            this.loginUser = loginUser;
            this.authorize = authorize;
            this.clientSource = clientSource;
        }
    }

    public bool TryGet(out string loginUser, out string authorize, out string clientSource)
    {
        lock (sync)
        {
            loginUser = this.loginUser ?? string.Empty;
            authorize = this.authorize ?? string.Empty;
            clientSource = this.clientSource ?? string.Empty;
            return !string.IsNullOrWhiteSpace(loginUser) &&
                   !string.IsNullOrWhiteSpace(authorize) &&
                   !string.IsNullOrWhiteSpace(clientSource);
        }
    }
}

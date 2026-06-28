namespace TIKR.Web.Services;

public sealed class LocalConnectionStateService
{
    public bool ApiOffline { get; private set; }

    public event Action? Changed;

    public void SetApiOffline(bool offline)
    {
        if (ApiOffline == offline)
            return;

        ApiOffline = offline;
        Changed?.Invoke();
    }
}

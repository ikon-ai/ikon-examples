using System.Diagnostics;
using System.Runtime.InteropServices;

public partial class Validation
{
    private readonly Reactive<string> _memoryAllocateMb = new("10");
    private readonly Reactive<int> _memoryAllocationVersion = new(0);
    private readonly Reactive<bool> _memoryAllocating = new(false);
    private readonly Reactive<bool> _memoryErrorToastOpen = new(false);
    private readonly Reactive<string> _memoryErrorMessage = new("");
    private readonly List<nint> _memoryUnmanagedAllocations = [];
    private readonly List<byte[]> _memoryManagedAllocations = [];
    private long _memoryUnmanagedBytes;

    private void RenderMemorySection(UIView view)
    {
        _ = _memoryAllocationVersion.Value;
        bool allocating = _memoryAllocating.Value;
        double processMemoryMb = Process.GetCurrentProcess().PrivateMemorySize64 / 1024.0 / 1024.0;
        double managedMemoryMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        double unmanagedAllocatedMb = _memoryUnmanagedBytes / 1024.0 / 1024.0;
        double managedAllocatedMb = _memoryManagedAllocations.Sum(a => a.Length) / 1024.0 / 1024.0;

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Row(["flex items-center gap-2 mb-4"], content: view =>
                {
                    view.Text([Text.H2], "Memory Info");
                    view.Button([Button.GhostMd, Button.Size.Icon],
                        onClick: async () => _memoryAllocationVersion.Value++,
                        content: v => v.Icon([Icon.Default], name: "refresh-cw"));
                });
                view.Text([Text.Body], $"Configured limit: {app.MaxMemoryLimitMb} MB");
                view.Text([Text.Body], $"Process memory: {processMemoryMb:F1} MB");
                view.Text([Text.Body], $"Managed memory: {managedMemoryMb:F1} MB");

                if (app.MaxMemoryLimitMb > 0 && processMemoryMb > app.MaxMemoryLimitMb)
                {
                    view.Text([Text.Body, "text-red-500 font-bold mt-2"],
                        $"WARNING: Process memory exceeds limit by {processMemoryMb - app.MaxMemoryLimitMb:F1} MB!");
                }
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Memory Allocation Test");
                view.Text([Text.Caption, "mb-4"], "Allocate memory to test memory warnings and container kills");

                view.Row(["flex items-center gap-4"], content: view =>
                {
                    view.Text([Text.Body], "MB to allocate:");
                    view.TextField([Input.Default, "w-32"], value: _memoryAllocateMb.Value, type: "number",
                        step: "1", min: "1", max: "4096",
                        onValueChange: async v => _memoryAllocateMb.Value = v ?? "10");
                    view.Button([Button.PrimaryMd], label: "Allocate Managed", disabled: allocating, onClick: AllocateManagedMemoryAsync);
                    view.Button([Button.PrimaryMd], label: "Allocate Unmanaged", disabled: allocating, onClick: AllocateUnmanagedMemoryAsync);
                    view.Button([Button.ErrorMd], label: "Free All", disabled: allocating, onClick: FreeAllMemoryAsync);
                });

                view.Text([Text.Body, "mt-4"], $"Managed allocations: {_memoryManagedAllocations.Count} ({managedAllocatedMb:F1} MB)");
                view.Text([Text.Body], $"Unmanaged allocations: {_memoryUnmanagedAllocations.Count} ({unmanagedAllocatedMb:F1} MB)");
            });

            view.Toast(
                open: _memoryErrorToastOpen.Value,
                onOpenChange: async open => _memoryErrorToastOpen.Value = open ?? false,
                durationMs: 5000,
                title: "Allocation Failed",
                description: _memoryErrorMessage.Value,
                showClose: true,
                toastStyle: [Toast.Base],
                viewportStyle: [Toast.ViewportBottomCenter],
                titleStyle: [Toast.Title],
                descriptionStyle: [Toast.Description],
                closeStyle: [Toast.Close]);
        });
    }

    private async Task AllocateManagedMemoryAsync()
    {
        if (!int.TryParse(_memoryAllocateMb.Value, out var mb) || mb <= 0 || mb > 2047)
        {
            return;
        }

        _memoryAllocating.Value = true;

        try
        {
            await Task.Run(() =>
            {
                var data = GC.AllocateUninitializedArray<byte>(mb * 1024 * 1024);
                _memoryManagedAllocations.Add(data);
            });
        }
        catch (OutOfMemoryException)
        {
            _memoryErrorMessage.Value = $"Out of memory when allocating {mb} MB of managed memory";
            _memoryErrorToastOpen.Value = true;
        }

        _memoryAllocating.Value = false;
        _memoryAllocationVersion.Value++;
    }

    private async Task AllocateUnmanagedMemoryAsync()
    {
        if (!int.TryParse(_memoryAllocateMb.Value, out var mb) || mb <= 0)
        {
            return;
        }

        _memoryAllocating.Value = true;

        try
        {
            await Task.Run(() =>
            {
                long bytes = (long)mb * 1024 * 1024;
                var ptr = Marshal.AllocHGlobal((nint)bytes);
                _memoryUnmanagedAllocations.Add(ptr);
                _memoryUnmanagedBytes += bytes;
            });
        }
        catch (OutOfMemoryException)
        {
            _memoryErrorMessage.Value = $"Out of memory when allocating {mb} MB of unmanaged memory";
            _memoryErrorToastOpen.Value = true;
        }

        _memoryAllocating.Value = false;
        _memoryAllocationVersion.Value++;
    }

    private async Task FreeAllMemoryAsync()
    {
        _memoryAllocating.Value = true;

        await Task.Run(() =>
        {
            _memoryManagedAllocations.Clear();

            foreach (var ptr in _memoryUnmanagedAllocations)
            {
                Marshal.FreeHGlobal(ptr);
            }

            _memoryUnmanagedAllocations.Clear();
            _memoryUnmanagedBytes = 0;

            GC.Collect();
        });

        _memoryAllocating.Value = false;
        _memoryAllocationVersion.Value++;
    }
}

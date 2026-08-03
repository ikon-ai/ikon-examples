using System.Runtime;
using System.Runtime.InteropServices;

public partial class Validation
{
    private readonly Reactive<string> _memoryAllocateMb = new("10");
    private readonly Reactive<int> _memoryAllocationVersion = new(0);
    private readonly Reactive<int> _maxClientsVersion = new(0);
    private readonly Reactive<string> _maxClientsOverride = new("");
    private readonly Reactive<bool> _memoryAllocating = new(false);
    private readonly Reactive<double> _memoryAfterGcProcessMb = new(0);
    private readonly Reactive<double> _memoryAfterGcManagedMb = new(0);
    private readonly Reactive<bool> _memoryErrorToastOpen = new(false);
    private readonly Reactive<string> _memoryErrorMessage = new("");
    private readonly List<nint> _memoryUnmanagedAllocations = [];
    private readonly List<byte[]> _memoryManagedAllocations = [];
    private long _memoryUnmanagedBytes;

    private void RenderMemorySection(UIView view)
    {
        _ = _memoryAllocationVersion.Value;
        bool allocating = _memoryAllocating.Value;
        double processMemoryMb = DiagnosticUtils.GetProcessMemoryUsedBytes() / 1024.0 / 1024.0;
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
                    view.Button([Button.GhostMd, Button.Icon],
                        text: "Run GC",
                        disabled: allocating,
                        onClick: ForceFullGcAsync,
                        content: v => v.Icon([Icon.Default], name: "refresh-cw"));
                });
                view.Text([Text.Body], $"Configured limit: {app.MaxMemoryLimitMb} MB");
                view.Text([Text.Body], $"Process memory: {processMemoryMb:F1} MB");
                view.Text([Text.Body], $"Managed memory: {managedMemoryMb:F1} MB");

                // Captured inside the GC handler the moment the blocking collection finishes,
                // BEFORE any re-render/transport churn re-allocates — the live "Managed memory"
                // figure above can read tens of MB high right after heavy connect/disconnect
                // traffic even though the retained heap is small. This pair is the stable signal
                // for footprint comparisons (used by the platform validation memory phase).
                if (_memoryAfterGcManagedMb.Value > 0)
                {
                    view.Text([Text.Body], $"Process after last GC: {_memoryAfterGcProcessMb.Value:F1} MB");
                    view.Text([Text.Body], $"Managed after last GC: {_memoryAfterGcManagedMb.Value:F1} MB");
                }

                if (app.MaxMemoryLimitMb > 0 && processMemoryMb > app.MaxMemoryLimitMb)
                {
                    view.Text([Text.Body, "text-red-500 font-bold mt-2"],
                        $"WARNING: Process memory exceeds limit by {processMemoryMb - app.MaxMemoryLimitMb:F1} MB!");
                }
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                _ = _maxClientsVersion.Value;
                int connectedClients = app.ReactiveGlobalState.Clients.Value.Values.Count(context => !context.IsInternal);

                view.Text([Text.H2, "mb-4"], "Client Limit");
                view.Text([Text.Body], $"Max clients: {(app.MaxClients > 0 ? app.MaxClients.ToString() : "unlimited")}");
                view.Text([Text.Body], $"Connected clients: {connectedClients}");

                view.Row(["flex items-center gap-4 mt-4"], content: view =>
                {
                    view.Text([Text.Body], "Override max clients:");
                    view.TextField([Input.Default, "w-32"], value: _maxClientsOverride.Value, type: "number",
                        step: "1", min: "0", placeholder: app.MaxClients.ToString(),
                        onValueChange: async v => _maxClientsOverride.Value = v ?? "");
                    view.Button([Button.PrimaryMd], text: "Apply", onClick: ApplyMaxClientsOverrideAsync);
                });

                view.Text([Text.Caption, "mt-2"],
                    "Set the limit to test connection rejection. 0 reverts to the server's memory-derived default; any other value (lower or higher) overrides it.");
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
                    view.Button([Button.PrimaryMd], text: "Allocate Managed", disabled: allocating, onClick: AllocateManagedMemoryAsync);
                    view.Button([Button.PrimaryMd], text: "Allocate Unmanaged", disabled: allocating, onClick: AllocateUnmanagedMemoryAsync);
                    view.Button([Button.ErrorMd], text: "Free All", disabled: allocating, onClick: FreeAllMemoryAsync);
                });

                view.Text([Text.Body, "mt-4"], $"Managed allocations: {_memoryManagedAllocations.Count} ({managedAllocatedMb:F1} MB)");
                view.Text([Text.Body], $"Unmanaged allocations: {_memoryUnmanagedAllocations.Count} ({unmanagedAllocatedMb:F1} MB)");
            });

            view.Toast(
                open: _memoryErrorToastOpen.Value,
                onOpenChange: async open => _memoryErrorToastOpen.Value = open,
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

    private async Task ApplyMaxClientsOverrideAsync()
    {
        if (int.TryParse(_maxClientsOverride.Value, out var maxClients) && maxClients >= 0)
        {
            app.MaxClients = maxClients;
            _maxClientsVersion.Value++;
        }

        await Task.CompletedTask;
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
                // The .NET GC pre-commits LOH segments with demand-zero pages and skips
                // the redundant memset on huge allocations (trusting the OS zero
                // guarantee). Without touching the pages, the array exists in the heap
                // accounting but isn't backed by physical RAM and doesn't show up in
                // RSS / cgroup pressure. Touch one byte per 4 KB page to force commit.
                var data = GC.AllocateUninitializedArray<byte>(mb * 1024 * 1024);
                const int pageSize = 4096;

                for (int offset = 0; offset < data.Length; offset += pageSize)
                {
                    data[offset] = 0;
                }

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

                // Linux malloc only reserves address space; physical pages aren't
                // committed until first write. Touch one byte per 4 KB page so the
                // allocation shows up in RSS and actually exercises the cgroup limit.
                const int pageSize = 4096;

                for (long offset = 0; offset < bytes; offset += pageSize)
                {
                    Marshal.WriteByte(ptr + (nint)offset, 0);
                }

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

    private async Task ForceFullGcAsync()
    {
        _memoryAllocating.Value = true;

        try
        {
            await Task.Run(() =>
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                _memoryAfterGcProcessMb.Value = DiagnosticUtils.GetProcessMemoryUsedBytes() / 1024.0 / 1024.0;
                _memoryAfterGcManagedMb.Value = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            });
        }
        finally
        {
            _memoryAllocating.Value = false;
            _memoryAllocationVersion.Value++;
        }
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

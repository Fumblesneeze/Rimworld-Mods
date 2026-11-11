using System;
using System.Runtime.InteropServices;

namespace Megamod.EndToEndTests.Diagnostics;

// A Windows-only precondition/cleanup observer for this native Copy scenario, not a clipboard service.
internal static class NativeDiagnosticClipboard
{
    internal sealed class Snapshot
    {
        internal Snapshot(uint sequence, bool supported, bool empty, string? text)
        { Sequence = sequence; Supported = supported; Empty = empty; Text = text; }
        internal uint Sequence { get; }
        internal bool Supported { get; }
        internal bool Empty { get; }
        internal string? Text { get; }
    }

    internal static Snapshot Observe(int maximumCharacters)
    {
        if (maximumCharacters < 0 || maximumCharacters > 256 * 1024) throw new ArgumentOutOfRangeException(nameof(maximumCharacters));
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Native clipboard format observation requires Windows.");
        if (!OpenClipboard(IntPtr.Zero)) throw new InvalidOperationException("Clipboard is busy; no copy or restoration is permitted.");
        uint sequence;
        bool empty = true;
        bool unicode = false;
        bool supported = true;
        string? text = null;
        try
        {
            sequence = GetClipboardSequenceNumber();
            if (sequence == 0) throw new InvalidOperationException("Clipboard sequence ownership is unavailable.");
            uint format = 0;
            for (int count = 0; ; count++)
            {
                format = EnumClipboardFormats(format);
                if (format == 0)
                {
                    if (Marshal.GetLastPInvokeError() != 0) throw new InvalidOperationException("Clipboard formats could not be inspected.");
                    break;
                }
                empty = false;
                // Windows enumerates synthesized ANSI/OEM text and locale alongside Unicode text.
                // All registered/custom formats, including HTML/RTF, images and file lists, are rejected.
                if (count >= 4 || format is not (1 or 7 or 13 or 16)) { supported = false; break; }
                if (format == 13) unicode = true;
            }
            supported &= empty || unicode;
            if (supported) text = empty ? string.Empty : ReadUnicodeWhileOpen(maximumCharacters);
            RequireSequence(sequence);
        }
        finally
        {
            if (!CloseClipboard()) throw new InvalidOperationException("Clipboard inspection lock could not be released.");
        }
        RequireSequence(sequence);
        if (!supported) return new Snapshot(sequence, false, false, null);
        return new Snapshot(sequence, true, empty, text);
    }

    private static string ReadUnicodeWhileOpen(int maximumCharacters)
    {
        // A failed provider getter must never be interpreted as valid empty text. The clipboard owns
        // this HGLOBAL: inspect only while OpenClipboard is held, unlock it and never free it.
        IntPtr handle = GetClipboardData(13); // CF_UNICODETEXT
        if (handle == IntPtr.Zero) throw new InvalidOperationException("Unicode clipboard data is unavailable; no valid text snapshot was read.");
        ulong bytes = GlobalSize(handle).ToUInt64();
        if (bytes < 2) throw new InvalidOperationException("Unicode clipboard allocation is unavailable or too small for a terminator.");
        IntPtr address = GlobalLock(handle);
        if (address == IntPtr.Zero) throw new InvalidOperationException("Unicode clipboard data could not be locked; no valid text snapshot was read.");
        try
        {
            int count = (int)Math.Min(bytes / 2, (ulong)maximumCharacters + 1);
            var characters = new char[count];
            try
            {
                Marshal.Copy(address, characters, 0, count);
                int terminator = Array.IndexOf(characters, '\0');
                if (terminator < 0) throw new InvalidOperationException("Unicode clipboard text has no terminator within the bounded allocation read.");
                return new string(characters, 0, terminator);
            }
            finally { Array.Clear(characters, 0, characters.Length); }
        }
        finally
        {
            // FALSE with NO_ERROR means the final lock was released successfully.
            if (!GlobalUnlock(handle) && Marshal.GetLastPInvokeError() != 0)
                throw new InvalidOperationException("Unicode clipboard memory could not be unlocked.");
        }
    }

    internal static void RequireUnchanged(Snapshot snapshot, int maximumCharacters)
    {
        Snapshot current = Observe(maximumCharacters);
        if (!current.Supported || current.Sequence != snapshot.Sequence || current.Empty != snapshot.Empty ||
            !string.Equals(current.Text, snapshot.Text, StringComparison.Ordinal))
            throw new InvalidOperationException("Clipboard changed after the plain-text/empty precondition; native Copy was not authorized by this observation.");
    }

    internal static void RestoreEmptyIfUnchanged(Snapshot owned)
    {
        if (!OpenClipboard(IntPtr.Zero)) throw new InvalidOperationException("Clipboard is busy; empty-state restoration remains incomplete.");
        try
        {
            RequireSequence(owned.Sequence);
            if (!EmptyClipboard()) throw new InvalidOperationException("Native empty-state restoration failed; cleanup remains incomplete.");
        }
        finally
        {
            if (!CloseClipboard()) throw new InvalidOperationException("Clipboard restoration lock could not be released.");
        }
    }

    private static void RequireSequence(uint expected)
    {
        if (GetClipboardSequenceNumber() != expected)
            throw new InvalidOperationException("Clipboard changed during observation; no stable ownership receipt is available.");
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint EnumClipboardFormats(uint previous);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();
}

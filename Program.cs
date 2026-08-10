using System.Windows.Forms;

namespace PointingKeys;

internal static class Program
{
    private const string MutexName = "Local\\PointingKeys.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "PointingKeys가 이미 실행 중입니다. 트레이 아이콘을 확인하세요.",
                "PointingKeys",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext());

        GC.KeepAlive(mutex);
    }
}

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RobotSelfBalancing.Communication;
using RobotSelfBalancing.Domain;
using RobotSelfBalancing.Persistence;

namespace RobotSelfBalancing.UI
{
    public partial class MainWindow : Window
    {
        private SerialPort? _serialPort;
        private readonly Channel<Telemetry> _channel;
        private readonly TelemetryRepository _repository;
        private CancellationTokenSource? _cts;

        private readonly List<double> _timeBuffer = new();
        private readonly List<double> _pitchBuffer = new();
        private readonly List<double> _uBuffer = new();

        private readonly DispatcherTimer _uiTimer;
        private int _currentSessionId = 0;
        private long _totalReceived = 0;
        private long _totalCorrupt = 0;
        private int _lastSeq = -1;
        private long _lostFramesCount = 0;

        private const string ConnectionString = "Server=localhost;Database=robotBalancinDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public MainWindow()
        {
            InitializeComponent();

            _repository = new TelemetryRepository(ConnectionString);

            _channel = Channel.CreateUnbounded<Telemetry>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true
            });

            CmbPuertos.ItemsSource = SerialPort.GetPortNames();
            if (CmbPuertos.Items.Count > 0) CmbPuertos.SelectedIndex = 0;

            CmbBaudios.ItemsSource = new string[] { "9600", "57600", "115200" };
            CmbBaudios.SelectedIndex = 2;

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _uiTimer.Tick += UiTimer_Tick;
        }

        private async void BtnConectar_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPuertos.SelectedItem == null) return;

            try
            {
                string portName = CmbPuertos.SelectedItem.ToString()!;
                int baudRate = int.Parse(CmbBaudios.SelectedItem.ToString()!);

                _serialPort = new SerialPort(portName, baudRate)
                {
                    NewLine = "\n",
                    ReadTimeout = 500
                };

                _serialPort.DataReceived += OnSerialDataReceived;
                _serialPort.Open();

                _cts = new CancellationTokenSource();

                _currentSessionId = await _repository.CrearSesionAsync("v1.0.0", 12.5f, 1.2f, 0.4f, 0.95f, 100, "Sesión activa");

                _ = Task.Run(() => ConsumerLoopAsync(_cts.Token));

                _uiTimer.Start();

                BtnConectar.IsEnabled = false;
                BtnDesconectar.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar: {ex.Message}");
            }
        }

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            while (_serialPort.BytesToRead > 0)
            {
                try
                {
                    string line = _serialPort.ReadLine();
                    if (FrameParser.TryParse(line, out var t))
                    {
                        _totalReceived++;
                        if (_lastSeq != -1 && t!.Seq > _lastSeq + 1)
                        {
                            _lostFramesCount += (t.Seq - _lastSeq - 1);
                        }
                        _lastSeq = t!.Seq;

                        _channel.Writer.TryWrite(t);
                    }
                    else
                    {
                        _totalCorrupt++;
                    }
                }
                catch { }
            }
        }

        private async Task ConsumerLoopAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            var lote = new List<Telemetry>(256);

            while (await timer.WaitForNextTickAsync(ct))
            {
                while (_channel.Reader.TryRead(out var t))
                {
                    lote.Add(t);
                }

                if (lote.Count > 0)
                {
                    await _repository.BulkInsertAsync(_currentSessionId, lote, ct);
                    lote.Clear();
                }
            }
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            while (_channel.Reader.TryPeek(out var t))
            {
                double sec = t.TiempoMs / 1000.0;
                _timeBuffer.Add(sec);
                _pitchBuffer.Add(t.PitchDeg);
                _uBuffer.Add(t.U);

                TxtVBat.Text = $"Batería: {t.VBat:F1} V";

                while (_timeBuffer.Count > 0 && (sec - _timeBuffer[0]) > 10.0)
                {
                    _timeBuffer.RemoveAt(0);
                    _pitchBuffer.RemoveAt(0);
                    _uBuffer.RemoveAt(0);
                }
                break;
            }

            long totalExpected = _totalReceived + _lostFramesCount;
            double lossPct = totalExpected > 0 ? (100.0 * _lostFramesCount / totalExpected) : 0;
            TxtLoss.Text = $"Pérdida: {lossPct:F1}% (Corruptas: {_totalCorrupt})";

            if (_timeBuffer.Count > 0)
            {
                PlotPitch.Plot.Clear();
                PlotPitch.Plot.Add.Scatter(_timeBuffer.ToArray(), _pitchBuffer.ToArray());
                PlotPitch.Refresh();

                PlotControl.Plot.Clear();
                PlotControl.Plot.Add.Scatter(_timeBuffer.ToArray(), _uBuffer.ToArray());
                PlotControl.Refresh();
            }
        }

        private void BtnParo_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.WriteLine("$DISARM");
                MessageBox.Show("¡Comando $DISARM enviado!", "PARO DE EMERGENCIA");
            }
        }

        private void BtnEnviarPID_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show("Debe estar conectado al robot.");
                return;
            }

            string cmd = $"$PID,{TxtKp.Text},{TxtKi.Text},{TxtKd.Text},{TxtAlpha.Text},{TxtThetaRef.Text}";
            _serialPort.WriteLine(cmd);
        }

        private async void BtnDesconectar_Click(object sender, RoutedEventArgs e)
        {
            _uiTimer.Stop();
            _cts?.Cancel();

            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            if (_currentSessionId > 0)
            {
                await _repository.FinalizarSesionAsync(_currentSessionId);
            }

            BtnConectar.IsEnabled = true;
            BtnDesconectar.IsEnabled = false;
        }
    }
}
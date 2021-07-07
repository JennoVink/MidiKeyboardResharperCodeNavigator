using AudioSwitcher.AudioApi.CoreAudio;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Newtonsoft.Json;
using PureMidi.CoreMmSystem.MidiIO.Data;
using PureMidi.CoreMmSystem.MidiIO.Definitions;
using PureMidi.CoreMmSystem.MidiIO.DeviceInfo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InputDevice = PureMidi.CoreMmSystem.MidiIO.InputDevice;

namespace MidiKeyboardResharperCodeNavigator.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private InputDevice _inputDevice;

        public bool IsConnected { get; set; }



        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            SoundEntries.Add(new SoundEntry(0, "28", new Uri(@"C:\sounds\fuifje1.mp3")));

            Console.WriteLine(Properties.Settings.Default.SoundboardSettings);
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SoundboardSettings))
                SoundEntries = JsonConvert.DeserializeObject<ObservableCollection<SoundEntry>>(Properties.Settings.Default.SoundboardSettings);

            OpenConnectionCommand = new RelayCommand(OpenConnectionCommandExecuted);
            RecordButtonCommand = new RelayCommand<string>((soundId) => RecordButtonCommandExecuted(soundId));
            RemoveSoundCommand = new RelayCommand<string>((soundId) => RemoveSoundCommandExecuted(soundId));
            AddNewKeyCommand = new RelayCommand(AddNewKeyCommandExecuted);
            RecordStopButtonCommand = new RelayCommand(RecordStopButtonCommandExecuted);
            RecordVolumeKnobCommand = new RelayCommand(RecordVolumeKnobCommandExecuted);

            IsRecording = new Dictionary<string, bool>
            {
                { "sound", false},
                { "stop", false},
                { "volume", false},
            };

            SpecialButtons = new Dictionary<string, string>
            {
                {"stop", "03"},
                { "volume", "02" }
            };

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SpecialButtons))
                SpecialButtons = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.SpecialButtons);

            MonitorLoad(this, EventArgs.Empty);

#if DEBUG
            OpenConnectionCommandExecuted();
#endif
        }

        private void RecordVolumeKnobCommandExecuted()
        {
            IsRecording["volume"] = true;
            RaisePropertyChanged(nameof(IsRecording));
        }

        private void SetVolume(int volume)
        {
            List<int> list = new List<int> { 2, 5, 7, 10 };
            int number = 9;

            int closest = SoundEntries.Select(x => x.Id * 10).Aggregate((x, y) => Math.Abs(x - volume) < Math.Abs(y - volume) ? x : y);
            Console.WriteLine(closest);

            PlaySound(closest / 10);
        }

        private void RecordStopButtonCommandExecuted()
        {
            IsRecording["stop"] = true;
            RaisePropertyChanged(nameof(IsRecording));
        }

        private void SaveSettings(CancelEventArgs cancelEventArgs)
        {
            Properties.Settings.Default.SoundboardSettings = JsonConvert.SerializeObject(SoundEntries);
            Properties.Settings.Default.SpecialButtons = JsonConvert.SerializeObject(SpecialButtons);
            Properties.Settings.Default.Save();
        }

        private void AddNewKeyCommandExecuted()
        {
            SoundEntries.Add(new SoundEntry((SoundEntries.LastOrDefault()?.Id ?? -1) + 1));
        }

        private void RemoveSoundCommandExecuted(string soundId)
        {
            SoundEntries.Remove(SoundEntries.First(x => x.Id == int.Parse(soundId)));
        }

        public int RecordingForSoundEntryId { get; set; } = -1;

        public ICommand OpenConnectionCommand { get; set; }
        public RelayCommand<string> SelectSoundPathCommand { get; set; }
        public RelayCommand<string> RecordButtonCommand { get; set; }
        public RelayCommand<string> RemoveSoundCommand { get; set; }
        public ICommand AddNewKeyCommand { get; set; }
        public ICommand RecordStopButtonCommand { get; set; }

        private int lastPlayedIndex = -1;

        // index 'sound': normal recording of a sound button
        // index 'stop': recording of the stop button
        // index 'volume': recording of a volume knob
        public Dictionary<string, bool> IsRecording { get; set; }

        public bool IgnoreAutoSensingSignals { get; set; }

        public ObservableCollection<MidiInInfo> InputDevices { get; set; } = new ObservableCollection<MidiInInfo>();

        public ObservableCollection<SoundEntry> SoundEntries { get; set; } = new ObservableCollection<SoundEntry>();

        // buttons like the stop button, set volume knob, etc (depends on what later is added in the future.
        public Dictionary<string, string> SpecialButtons { get; set; }

        public MidiInInfo SelectedInputDevice { get; set; }

        //public ICommand WindowClosing => new RelayCommand<CancelEventArgs>(SaveSettings);

        public ICommand WindowClosing =>
            new RelayCommand<CancelEventArgs>(
                SaveSettings);

        public ICommand RecordVolumeKnobCommand { get; set; }

        private void OpenConnectionCommandExecuted()
        {
            if (!IsConnected)
            {
                SwitchMonitorOn();
            }
            else
            {
                SwitchMonitorOff();
            }
        }

        private void SwitchMonitorOn()
        {
            SwitchMonitorOff();
            if (SelectedInputDevice != null)
            {
                _inputDevice = new InputDevice(SelectedInputDevice.DeviceIndex);
                _inputDevice.OnMidiEvent += OnMidiEventHandle;
                _inputDevice.Start();

                IsConnected = true;
            }
            else
            {
                MessageBox.Show("input device must be selected.");
            }
        }

        private void OnMidiEventHandle(MidiEvent ev)
        {
            if (ev.MidiEventType == EMidiEventType.Short)
            {
                //Console.WriteLine(ev.Hex + " |  "
                //                         + ev.Status.ToString("X2").ToUpper() + "  |  " +
                //                         (ev.Status & 0xF0).ToString("X2").ToUpper() + " | " +
                //                         ((ev.Status & 0x0F) + 1).ToString("X2").ToUpper() + " |  " +
                //                         ev.AllData[1].ToString("X2").ToUpper() + "   |  " +
                //                         ev.AllData[2].ToString("X2").ToUpper() + "   | ");

                // ignore autosensing
                if (ev.Hex == "FE0000" && IgnoreAutoSensingSignals)
                    return;

                if (IsRecording["volume"])
                {
                    SpecialButtons["volume"] = ev.AllData[1].ToString("X2");
                    IsRecording["volume"] = false;
                    RaisePropertyChanged(nameof(IsRecording));
                }
                if (IsRecording["stop"])
                {
                    SpecialButtons["stop"] = ev.AllData[1].ToString("X2");
                    Debug.WriteLine($"Stop button recording ended {ev.AllData[1]:X2}");

                    IsRecording["stop"] = false;
                    RaisePropertyChanged(nameof(IsRecording));
                }
                else if (IsRecording["sound"])
                {
                    IsRecording["sound"] = false;
                    Debug.WriteLine($"recording ended {ev.AllData[1]:X2}");

                    SoundEntries.First(x => x.Id == RecordingForSoundEntryId).KeyId = ev.AllData[1].ToString("X2");

                    // The frontend needs a MultiBinding, checking if RecordingForSoundEntryId equal is to the current Id of the SoundEntry.
                    // In the most optimal solution, the IsRecording["sound"] is also checked in the FE. Unfortunately this gave some problems.
                    // That's why it is set to -1.
                    RecordingForSoundEntryId = -1;
                    RaisePropertyChanged(nameof(IsRecording));
                }
                else if (ev.Status == 144) // pressed button
                {
                    PlaySound(ev.AllData[1].ToString("X2"));
                }
                else if (ev.AllData[1].ToString("X2") == SpecialButtons["stop"])
                {
                    StopAllSounds();
                }
                else if (ev.AllData[1].ToString("X2") == SpecialButtons["volume"])
                {
                    SetVolume((int)(int.Parse(ev.AllData[2].ToString()) / 127.0 * 100));
                }
            }
        }


        private void RecordButtonCommandExecuted(string soundEntryId)
        {
            IsRecording["sound"] = true;
            RecordingForSoundEntryId = int.Parse(soundEntryId);
            RaisePropertyChanged(nameof(IsRecording));

            Debug.WriteLine("recording started");
        }

        private void PlaySound(string midiKeyIndex)
        {
            var soundEntry = SoundEntries.FirstOrDefault(x => x.KeyId == midiKeyIndex);
            if (soundEntry != null)
            {
                soundEntry.Play(lastPlayedIndex == soundEntry.Id);
                lastPlayedIndex = soundEntry.Id;
            }
        }

        private void PlaySound(int slotId)
        {
            var soundEntry = SoundEntries.FirstOrDefault(x => x.Id == slotId);
            if (soundEntry != null)
            {
                soundEntry.Play(lastPlayedIndex == soundEntry.Id);
                lastPlayedIndex = soundEntry.Id;
            }
        }

        private void StopAllSounds()
        {
            foreach (var soundEntry in SoundEntries)
            {
                soundEntry.Stop();
            }
        }

        public void SwitchMonitorOff()
        {
            if (_inputDevice != null && !_inputDevice.IsDisposed) _inputDevice.Dispose();
            IsConnected = false;
        }

        private void MonitorLoad(object sender, EventArgs e)
        {
            var inp = MidiInInfo.Informations;
            foreach (var midiInInfo in inp)
            {
                InputDevices.Add(midiInInfo);
            }

            SelectedInputDevice = InputDevices.FirstOrDefault();
        }
    }
}

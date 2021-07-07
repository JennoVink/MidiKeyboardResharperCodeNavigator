using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace MidiKeyboardResharperCodeNavigator.ViewModel
{
    /// <summary>
    /// An entry representing a sound and a key (on the midi keyboard/pad).
    /// </summary>
    public class SoundEntry : ObservableObject
    {
        public int Id { get; set; }
        public string KeyId { get; set; }


        [JsonConstructor]
        public SoundEntry(int id, string keyId, Uri soundPath)
        {
            Id = id;
            KeyId = keyId;
        }

        public SoundEntry(int id) : this(id, "", new Uri(@"c:\")) { }

        public void Play(bool tappedTwice)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (tappedTwice)
                {
                    SendKeys.SendWait($"^+{Id + 1}");
                }
                else
                {
                    SendKeys.SendWait($"^{Id + 1}");
                }
            });

        }

        public void Stop()
        {
            // no-op
        }

    }
}

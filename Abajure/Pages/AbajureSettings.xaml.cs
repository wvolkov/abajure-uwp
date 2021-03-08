using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using AbataLibrary.Controllers;
using AbataLibrary.Entities;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Devices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Abajure
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AbajureSettingsPage : Page
    {
        private PlayerProvider _playerProvider;
        public AbajureSettingsPage()
        {
            this.InitializeComponent();
        }

        private async void InitializeUI()
        {
            _playerProvider = await PlayerProvider.GetPlayerProvider();
            FillAudioDeviceComboBox();
        }

        private async void FillAudioDeviceComboBox()
        {
            prAudioDevices.Visibility = Visibility.Visible;
            prAudioDevices.IsActive = true;
            cbAudioDevices.IsEnabled = false;
            string audioSelector = MediaDevice.GetAudioRenderSelector();
            DeviceInformationCollection outputDevices = await DeviceInformation.FindAllAsync(audioSelector);
            foreach (DeviceInformation device in outputDevices)
            {
                ComboBoxItem deviceItem = new ComboBoxItem();
                deviceItem.Content = device.Name;
                deviceItem.Tag = device;
                cbAudioDevices.Items.Add(deviceItem);
            }
            prAudioDevices.IsActive = false;
            prAudioDevices.Visibility = Visibility.Collapsed;
            cbAudioDevices.IsEnabled = true;

            if (_playerProvider.Settings.AudioDeviceId != null)
            {
                int inx = cbAudioDevices.Items.Cast<ComboBoxItem>().ToList().FindIndex(i => ((DeviceInformation)i.Tag).Id == _playerProvider.Settings.AudioDeviceId);
                cbAudioDevices.SelectedIndex = inx;
            }
        }

        private void ApBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cbAudioDevices.SelectedIndex != -1)
            {
                DeviceInformation selectedAudioDevice = (DeviceInformation)((ComboBoxItem)cbAudioDevices.SelectedItem).Tag;
                AbajureSettings settings = _playerProvider.Settings;
                settings.AudioDeviceId = selectedAudioDevice.Id;
                settings.Save();
                _playerProvider.SetMediaDevice(selectedAudioDevice);
                Frame.Navigate(typeof(PlaylistPage), "Back", new EntranceNavigationTransitionInfo());
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InitializeUI();
            // Register for hardware and software back request from the system
            SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested += OnBackRequested;
            systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            // Mark event as handled so we don't get bounced out of the app.
            e.Handled = true;
            // Page above us will be our master view.
            // Make sure we are using the "drill out" animation in this transition.
            Frame.Navigate(typeof(PlaylistPage), "Back", new EntranceNavigationTransitionInfo());
        }
    }
}

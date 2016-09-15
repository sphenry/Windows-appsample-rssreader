//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using RssReader.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace RssReader.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RemoteSystemsView : Page, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
        private RemoteSystemWatcher _remoteSystemWatcher;


        private MainViewModel ViewModel => AppShell.Current.ViewModel;
        public ObservableCollection<RemoteSystem> DeviceList { get; } = new ObservableCollection<RemoteSystem>();

        public RemoteSystemsView()
        {
            this.InitializeComponent();
            DiscoverDevices();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
        }

        private async void DiscoverDevices()
        {
            var accessStatus = await RemoteSystem.RequestAccessAsync();
            if (accessStatus == RemoteSystemAccessStatus.Allowed)
            {
                // _remoteSystemWatcher = RemoteSystem.CreateWatcher(new IRemoteSystemFilter[] { new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal) });
                _remoteSystemWatcher = RemoteSystem.CreateWatcher();


                //Add RemoteSystem to DeviceList (on the UI Thread)
                _remoteSystemWatcher.RemoteSystemAdded += async (sender, args) =>
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DeviceList.Add(args.RemoteSystem));

                //Remove RemoteSystem from DeviceList (on the UI Thread)
                _remoteSystemWatcher.RemoteSystemRemoved += async (sender, args) =>
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DeviceList.Remove(DeviceList.FirstOrDefault(system => system.Id == args.RemoteSystemId)));

                //Update RemoteSystem on DeviceList (on the UI Thread)
                _remoteSystemWatcher.RemoteSystemUpdated += async (sender, args) =>
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        DeviceList.Remove(DeviceList.FirstOrDefault(system => system.Id == args.RemoteSystem.Id));
                        DeviceList.Add(args.RemoteSystem);
                    });

                _remoteSystemWatcher.Start();
            }
        }

        private async void RemoteSystemList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRemoteSystem = RemoteSystemList.SelectedItem as RemoteSystem;
            var uri = ViewModel.CurrentArticle.Link;

            var launchUriStatus = await RemoteLauncher.LaunchUriAsync(new RemoteSystemConnectionRequest(selectedRemoteSystem), uri);

        }
    }
}


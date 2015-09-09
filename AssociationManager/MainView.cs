﻿// Copyright (c) 2015, Dijji, and released under Ms-PL.  This, with other relevant licenses, can be found in the root of this distribution.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace FileMetadataAssociationManager
{
    class MainView : INotifyPropertyChanged
    {
        private State state;
        private MainWindow window;
        private Profile selectedProfile = null;
        private List<Extension> selectedExtensions = new List<Extension>();
        private enum HandlerSet
        {
            None,
            Ours,
            Other,
        }
        private HandlerSet? handlersSelected;
        private bool sortRequired = false;

        public ObservableCollectionWithReset<Extension> Extensions { get { return state.Extensions; } }
        public string Restrictions { get { return state.Restrictions; } }
        public int RestrictionLevel { get { return state.RestrictionLevel; } }

        public ObservableCollection<TreeItem> FullDetails { get { return selectedProfile == null ? null : selectedProfile.FullDetails; } }
        public ObservableCollection<string> PreviewDetails { get { return selectedProfile == null ? null : selectedProfile.PreviewDetails; } }

        public List<Extension> SelectedExtensions { get { return selectedExtensions; } }

        public Profile SelectedProfile
        {
            get { return selectedProfile; }
            set
            {
                if (selectedProfile != value)
                {
                    selectedProfile = value;
                    OnPropertyChanged("SelectedProfile");
                    OnPropertyChanged("FullDetails");
                    OnPropertyChanged("PreviewDetails");
                }
            }
        }

        public IEnumerable<Profile> Profiles
        {
            get
            {
                foreach (Profile p in state.BuiltInProfiles)
                {
                    yield return p;
                }
                foreach (Profile p in state.CustomProfiles)
                {
                    yield return p;
                }
            }
        }

        public bool CanChooseProfile { get { return handlersSelected == HandlerSet.None; } }

        public bool CanAddPropertyHandlerEtc
        {
            get
            {
                return Extension.IsOurPropertyHandlerRegistered && SelectedProfile != null &&
                       handlersSelected == HandlerSet.None;
            }
        }

        public bool CanRemovePropertyHandlerEtc { get { return handlersSelected == HandlerSet.Ours; } }

        public bool SortRequired
        {
            get { return sortRequired; }
            set
            {
                if (value != sortRequired)
                {
                    sortRequired = value;
                    OnPropertyChanged("SortRequired");
                }
            }
        }
        
        public MainView(MainWindow window, State state)
        {
            this.window = window;
            this.state = state;
        }

        public void SetSelectedExtensions(System.Collections.IList selections)
        {
            SelectedExtensions.Clear();
            foreach (var s in selections)
                SelectedExtensions.Add((Extension)s);

            DeterminePossibleActions();
        }

        public bool AddHandlers()
        {
            bool success = true;

            if (SelectedExtensions.Count > 0 && SelectedProfile != null)
            {
                foreach (Extension ext in SelectedExtensions)
                {
                    success &= ext.SetupHandlerForExtension(SelectedProfile);
                }
            }

            DeterminePossibleActions();
            SortRequired = true;

            return success;
        }

        public void RemoveHandlers()
        {
            foreach (Extension ext in SelectedExtensions)
            {
                ext.RemoveHandlerFromExtension();
            }

            DeterminePossibleActions();
            SortRequired = true;
        }

        public void RefreshProfiles()
        {
            OnPropertyChanged("Profiles");
        }

        private void DeterminePossibleActions()
        {
            // Cases are:
            // 1. All selected extensions have no handler: profile combo box is enabled and profile property lists are shown. 
            // 2. All selected extensions have File Meta handler: profile combo box is disabled and profile for the 
            //    first selected extension is shown in combo box and profile property lists. 
            // 3. All other cases: profile combo box is disabled and profile property lists are empty.
            handlersSelected = null;
            foreach (Extension e in SelectedExtensions)
            {
                if (!e.HasHandler)
                {
                    if (handlersSelected == null)
                        handlersSelected = HandlerSet.None;
                    else if (handlersSelected == HandlerSet.None)
                        continue;
                    else
                    {
                        handlersSelected = HandlerSet.Other;
                        break;
                    }
                }
                else if (e.OurHandler)
                {
                    if (handlersSelected == null)
                        handlersSelected = HandlerSet.Ours;
                    else if (handlersSelected == HandlerSet.Ours)
                        continue;
                    else
                    {
                        handlersSelected = HandlerSet.Other;
                        break;
                    }
                }
                else
                {
                    handlersSelected = HandlerSet.Other;
                    break;
                }
            }
            if (handlersSelected == null)
                handlersSelected = HandlerSet.Other;

            switch (handlersSelected)
            {
                case HandlerSet.None:
                    if (SelectedProfile == null)
                        SelectedProfile = state.BuiltInProfiles.First();
                    break;
                case HandlerSet.Ours:
                    SelectedProfile = SelectedExtensions.First().Profile;
                    break;
                case HandlerSet.Other:
                    SelectedProfile = null;
                    break;
            }

            OnPropertyChanged("CanChooseProfile");
            OnPropertyChanged("CanAddPropertyHandlerEtc");
            OnPropertyChanged("CanRemovePropertyHandlerEtc");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}

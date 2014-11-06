// 
// Copyright (c) 2010-2014 Yves Langisch. All rights reserved.
// http://cyberduck.ch/
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// Bug fixes, suggestions and comments should be sent to:
// yves@cyberduck.ch
// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Ch.Cyberduck.Core;
using Ch.Cyberduck.Core.Local;
using Ch.Cyberduck.Ui.Controller.Threading;
using Ch.Cyberduck.Ui.Winforms;
using Ch.Cyberduck.Ui.Winforms.Taskdialog;
using StructureMap;
using ch.cyberduck.core;
using ch.cyberduck.core.cdn;
using ch.cyberduck.core.editor;
using ch.cyberduck.core.exception;
using ch.cyberduck.core.features;
using ch.cyberduck.core.local;
using ch.cyberduck.core.serializer;
using ch.cyberduck.core.sftp;
using ch.cyberduck.core.ssl;
using ch.cyberduck.core.threading;
using ch.cyberduck.core.transfer;
using ch.cyberduck.ui;
using ch.cyberduck.ui.action;
using ch.cyberduck.ui.browser;
using ch.cyberduck.ui.comparator;
using ch.cyberduck.ui.pasteboard;
using ch.cyberduck.ui.threading;
using java.lang;
using java.util;
using org.apache.log4j;
using Application = ch.cyberduck.core.local.Application;
using Boolean = java.lang.Boolean;
using DataObject = System.Windows.Forms.DataObject;
using Exception = System.Exception;
using Path = ch.cyberduck.core.Path;
using String = System.String;
using StringBuilder = System.Text.StringBuilder;

namespace Ch.Cyberduck.Ui.Controller
{
    public class BrowserController : WindowController<IBrowserView>, TranscriptListener, CollectionListener,
                                     ProgressListener
    {
        public delegate void CallbackDelegate();

        public delegate bool DialogCallbackDelegate(DialogResult result);

        internal static readonly Filter HiddenFilter = new RegexFilter();

        private static readonly Logger Log = Logger.getLogger(typeof (BrowserController).FullName);
        private static readonly Filter NullFilter = new NullPathFilter();
        protected static string DEFAULT = LocaleFactory.localizedString("Default");
        private readonly BookmarkCollection _bookmarkCollection = BookmarkCollection.defaultCollection();
        private readonly BookmarkModel _bookmarkModel;
        private readonly TreeBrowserModel _browserModel;
        private readonly Cache _cache = new Cache();
        private readonly Navigation _navigation = new Navigation();
        private readonly IList<FileSystemWatcher> _temporaryWatcher = new List<FileSystemWatcher>();
        private Comparator _comparator = new NullComparator();
        private String _dropFolder; // holds the drop folder of the current drag operation
        private InfoController _inspector;
        private BrowserView _lastBookmarkView = BrowserView.Bookmark;
        private PathPasteboard _pasteboard;

        /**
         * Caching files listings of previously listed directories
        */

        /*
         * No file filter.
         */
        private Session _session;
        private bool _showHiddenFiles;

        /**
         * Navigation history
        */

        public BrowserController(IBrowserView view)
        {
            View = view;

            ShowHiddenFiles = Preferences.instance().getBoolean("browser.showHidden");

            _browserModel = new TreeBrowserModel(this, _cache);
            _bookmarkModel = new BookmarkModel(this, _bookmarkCollection);
            View.ViewClosedEvent += delegate { _bookmarkModel.Source = null; };

            //default view is the bookmark view
            ToggleView(BrowserView.Bookmark);
            View.StopActivityAnimation();

            View.SetComparator += View_SetComparator;
            View.ChangeBrowserView += View_ChangeBrowserView;

            View.QuickConnect += View_QuickConnect;
            View.BrowserDoubleClicked += View_BrowserDoubleClicked;
            View.BrowserSelectionChanged += View_BrowserSelectionChanged;
            View.PathSelectionChanged += View_PathSelectionChanged;
            View.EditEvent += View_EditEvent;
            View.ItemsChanged += View_ItemsChanged;

            View.ShowTransfers += View_ShowTransfers;

            View.BrowserCanDrop += View_BrowserCanDrop;
            View.HostCanDrop += View_HostCanDrop;
            View.BrowserModelCanDrop += View_BrowserModelCanDrop;
            View.HostModelCanDrop += View_HostModelCanDrop;
            View.BrowserDropped += View_BrowserDropped;
            View.HostDropped += View_HostDropped;
            View.HostModelDropped += View_HostModelDropped;
            View.BrowserModelDropped += View_BrowserModelDropped;
            View.BrowserDrag += View_BrowserDrag;
            View.HostDrag += View_HostDrag;
            View.BrowserEndDrag += View_BrowserEndDrag;
            View.HostEndDrag += View_HostEndDrag;
            View.SearchFieldChanged += View_SearchFieldChanged;


            View.ContextMenuEnabled += View_ContextMenuEnabled;

            #region Commands - File

            View.NewBrowser += View_NewBrowser;
            View.ValidateNewBrowser += View_ValidateNewBrowser;
            View.OpenConnection += View_OpenConnection;
            View.ValidateOpenConnection += () => true;
            View.NewDownload += View_NewDownload;
            View.ValidateNewDownload += () => false; //todo implement
            View.NewFolder += View_NewFolder;
            View.ValidateNewFolder += View_ValidateNewFolder;
            View.NewFile += View_NewFile;
            View.ValidateNewFile += View_ValidateNewFile;
            View.NewSymbolicLink += View_NewSymbolicLink;
            View.ValidateNewSymbolicLink += View_ValidateNewSymbolicLink;
            View.RenameFile += View_RenameFile;
            View.ValidateRenameFile += View_ValidateRenameFile;
            View.DuplicateFile += View_DuplicateFile;
            View.ValidateDuplicateFile += View_ValidateDuplicateFile;
            View.OpenUrl += View_OpenUrl;
            View.ValidateOpenWebUrl += View_ValidateOpenWebUrl;
            View.ValidateEditWith += View_ValidateEditWith;
            View.ShowInspector += View_ShowInspector;
            View.ValidateShowInspector += View_ValidateShowInspector;
            View.Download += View_Download;
            View.ValidateDownload += View_ValidateDownload;
            View.DownloadAs += View_DownloadAs;
            View.ValidateDownloadAs += View_ValidateDownloadAs;
            View.DownloadTo += View_DownloadTo;
            View.ValidateDownloadTo += View_ValidateDownload; //use same validation handler
            View.Upload += View_Upload;
            View.ValidateUpload += View_ValidateUpload;
            View.Synchronize += View_Synchronize;
            View.ValidateSynchronize += View_ValidateSynchronize;
            View.Delete += View_Delete;
            View.ValidateDelete += View_ValidateDelete;
            View.RevertFile += View_RevertFile;
            View.ValidateRevertFile += View_ValidateRevertFile;
            View.GetArchives += View_GetArchives;
            View.GetCopyUrls += View_GetCopyUrls;
            View.GetOpenUrls += View_GetOpenUrls;
            View.CreateArchive += View_CreateArchive;
            View.ValidateCreateArchive += View_ValidateCreateArchive;
            View.ExpandArchive += View_ExpandArchive;
            View.ValidateExpandArchive += View_ValidateExpandArchive;

            #endregion

            #region Commands - Edit

            View.Cut += View_Cut;
            View.ValidateCut += View_ValidateCut;
            View.Copy += View_Copy;
            View.ValidateCopy += View_ValidateCopy;
            View.Paste += View_Paste;
            View.ValidatePaste += View_ValidatePaste;
            View.ShowPreferences += View_ShowPreferences;

            #endregion

            #region Commands - View

            View.ToggleToolbar += View_ToggleToolbar;
            View.ShowHiddenFiles += View_ShowHiddenFiles;
            View.ValidateTextEncoding += View_ValidateTextEncoding;
            View.EncodingChanged += View_EncodingChanged;
            View.ToggleLogDrawer += View_ToggleLogDrawer;

            #endregion

            #region Commands - Go

            View.RefreshBrowser += View_RefreshBrowser;
            View.ValidateRefresh += View_ValidateRefresh;
            View.GotoFolder += View_GotoFolder;
            View.ValidateGotoFolder += View_ValidateGotoFolder;
            View.HistoryBack += View_HistoryBack;
            View.ValidateHistoryBack += View_ValidateHistoryBack;
            View.HistoryForward += View_HistoryForward;
            View.ValidateHistoryForward += View_ValidateHistoryForward;
            View.FolderUp += View_FolderUp;
            View.ValidateFolderUp += View_ValidateFolderUp;
            View.FolderInside += View_FolderInside;
            View.ValidateFolderInside += View_ValidateFolderInside;
            View.Search += View_Search;
            View.SendCustomCommand += View_SendCustomCommand;
            View.ValidateSendCustomCommand += View_ValidateSendCustomCommand;
            View.OpenInTerminal += View_OpenInTerminal;
            View.ValidateOpenInTerminal += View_ValidateOpenInTerminal;
            View.Stop += View_Disconnect;
            View.ValidateStop += View_ValidateStop;
            View.Disconnect += View_Disconnect;
            View.ValidateDisconnect += View_ValidateDisconnect;

            #endregion

            #region Commands - Bookmark

            View.ToggleBookmarks += View_ToggleBookmarks;
            View.SortBookmarksByHostname += View_SortBookmarksByHostname;
            View.SortBookmarksByNickname += View_SortBookmarksByNickname;
            View.SortBookmarksByProtocol += View_SortBookmarksByProtocol;

            View.ConnectBookmark += View_ConnectBookmark;
            View.ValidateConnectBookmark += View_ValidateConnectBookmark;
            View.NewBookmark += View_NewBookmark;
            View.ValidateNewBookmark += View_ValidateNewBookmark;
            View.EditBookmark += View_EditBookmark;
            View.ValidateEditBookmark += View_ValidateEditBookmark;
            View.DeleteBookmark += View_DeleteBookmark;
            View.ValidateDeleteBookmark += View_ValidateDeleteBookmark;
            View.DuplicateBookmark += View_DuplicateBookmark;
            View.ValidateDuplicateBookmark += View_ValidateDuplicateBookmark;

            #endregion

            #region Browser model delegates

            View.ModelCanExpandDelegate = _browserModel.CanExpand;
            View.ModelChildrenGetterDelegate = _browserModel.ChildrenGetter;
            View.ModelFilenameGetter = _browserModel.GetName;
            View.ModelIconGetter = _browserModel.GetIcon;
            View.ModelSizeGetter = _browserModel.GetSize;
            View.ModelSizeAsStringGetter = _browserModel.GetSizeAsString;
            View.ModelModifiedGetter = _browserModel.GetModified;
            View.ModelModifiedAsStringGetter = _browserModel.GetModifiedAsString;
            View.ModelOwnerGetter = _browserModel.GetOwner;
            View.ModelGroupGetter = _browserModel.GetGroup;
            View.ModelPermissionsGetter = _browserModel.GetPermission;
            View.ModelKindGetter = _browserModel.GetKind;
            View.ModelActiveGetter = _browserModel.GetActive;
            View.ModelExtensionGetter = _browserModel.GetExtension;
            View.ModelRegionGetter = _browserModel.GetRegion;
            View.ModelVersionGetter = _browserModel.GetVersion;

            #endregion

            #region Bookmark model delegates

            View.BookmarkImageGetter = _bookmarkModel.GetBookmarkImage;
            View.BookmarkNicknameGetter = _bookmarkModel.GetNickname;
            View.BookmarkHostnameGetter = _bookmarkModel.GetHostname;
            View.BookmarkUrlGetter = _bookmarkModel.GetUrl;
            View.BookmarkNotesGetter = _bookmarkModel.GetNotes;
            View.BookmarkStatusImageGetter = _bookmarkModel.GetBookmarkStatusImage;

            #endregion

            _bookmarkCollection.addListener(this);
            View.ViewClosedEvent += delegate { _bookmarkCollection.removeListener(this); };

            PopulateQuickConnect();
            PopulateEncodings();
            UpdateOpenIcon();

            View.ToolbarVisible = Preferences.instance().getBoolean("browser.toolbar");
            View.LogDrawerVisible = Preferences.instance().getBoolean("browser.transcript.open");

            View.GetEditorsForSelection += View_GetEditorsForSelection;
            View.GetBookmarks += View_GetBookmarks;
            View.GetHistory += View_GetHistory;
            View.GetBonjourHosts += View_GetBonjourHosts;
            View.ClearHistory += View_ClearHistory;
            View.ShowCertificate += View_Certificate;

            View.ValidatePathsCombobox += View_ValidatePathsCombobox;
            View.ValidateSearchField += View_ValidateSearchField;

            View.Exit += View_Exit;
            View.SetBookmarkModel(_bookmarkCollection, null);
        }

        public BrowserController() : this(ObjectFactory.GetInstance<IBrowserView>())
        {
        }

        /// <summary>
        /// The first selected path found or null if there is no selection
        /// </summary>
        public Path SelectedPath
        {
            get
            {
                IList<Path> selectedPaths = View.SelectedPaths;
                if (selectedPaths.Count > 0)
                {
                    return selectedPaths[0];
                }
                return null;
            }
        }

        public Path Workdir { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <value>
        ///   All selected paths or an empty list if there is no selection
        /// </value>
        public IList<Path> SelectedPaths
        {
            get
            {
                if (IsMounted())
                {
                    return View.SelectedPaths;
                }
                return new List<Path>();
            }
            set { View.SelectedPaths = value; }
        }

        public bool ShowHiddenFiles
        {
            get { return _showHiddenFiles; }
            set
            {
                FilenameFilter = value ? NullFilter : HiddenFilter;
                _showHiddenFiles = value;
                View.HiddenFilesVisible = _showHiddenFiles;
            }
        }

        public Cache Cache
        {
            get { return _cache; }
        }

        public Filter FilenameFilter { get; set; }

        public Comparator FilenameComparator
        {
            get { return _comparator; }
            set { _comparator = value; }
        }

        public Session Session
        {
            get { return _session; }
        }

        public void collectionLoaded()
        {
            AsyncDelegate mainAction = delegate { ReloadBookmarks(); };
            Invoke(mainAction);
        }

        public void collectionItemAdded(object obj)
        {
            AsyncDelegate mainAction = delegate { PopulateQuickConnect(); };
            Invoke(mainAction);
        }

        public void collectionItemRemoved(object obj)
        {
            AsyncDelegate mainAction = delegate { PopulateQuickConnect(); };
            Invoke(mainAction);
        }

        public void collectionItemChanged(object obj)
        {
            AsyncDelegate mainAction = delegate { PopulateQuickConnect(); };
            Invoke(mainAction);
        }

        public override void message(string msg)
        {
            string label;
            if (Utils.IsNotBlank(msg))
            {
                label = msg;
            }
            else
            {
                if (View.CurrentView == BrowserView.Bookmark || View.CurrentView == BrowserView.History ||
                    View.CurrentView == BrowserView.Bonjour)
                {
                    label = String.Format("{0} {1}", View.NumberOfBookmarks, LocaleFactory.localizedString("Bookmarks"));
                }
                else
                {
                    if (IsConnected())
                    {
                        label = String.Format(LocaleFactory.localizedString("{0} Files"), View.NumberOfFiles);
                    }
                    else
                    {
                        label = String.Empty;
                    }
                }
            }
            AsyncDelegate updateLabel = delegate { View.StatusLabel = label; };
            Invoke(updateLabel);
        }

        public override void log(bool request, string transcript)
        {
            if (View.LogDrawerVisible)
            {
                AsyncDelegate mainAction = delegate { View.AddTranscriptEntry(request, transcript); };
                Invoke(mainAction);
            }
        }

        private void View_NewSymbolicLink()
        {
            CreateSymlinkController slc =
                new CreateSymlinkController(ObjectFactory.GetInstance<ICreateSymlinkPromptView>(), this);
            slc.Show();
        }

        private bool View_ValidateNewSymbolicLink()
        {
            return IsMounted() && _session.getFeature(typeof (Symlink)) != null && SelectedPaths.Count == 1;
        }

        private void View_SortBookmarksByProtocol()
        {
            BookmarkCollection.defaultCollection().sortByProtocol();
            ReloadBookmarks();
        }

        private void View_SortBookmarksByNickname()
        {
            BookmarkCollection.defaultCollection().sortByNickname();
            ReloadBookmarks();
        }

        private void View_SortBookmarksByHostname()
        {
            BookmarkCollection.defaultCollection().sortByHostname();
            ReloadBookmarks();
        }

        private bool View_ValidateOpenInTerminal()
        {
            return IsMounted() && Session is SFTPSession &&
                   File.Exists(Preferences.instance().getProperty("terminal.command.ssh"));
        }

        private void View_OpenInTerminal()
        {
            Host host = Session.getHost();
            Path workdir = null;
            if (SelectedPaths.Count == 1)
            {
                Path selected = SelectedPath;
                if (selected.isDirectory())
                {
                    workdir = selected;
                }
            }
            if (null == workdir)
            {
                workdir = Workdir;
            }
            new SshTerminalService().open(host, workdir);
        }

        private void View_SetComparator(BrowserComparator comparator)
        {
            if (!comparator.equals(_comparator))
            {
                _comparator = comparator;
                ReloadData(true);
            }
        }

        private IList<Application> View_GetEditorsForSelection()
        {
            Path p = SelectedPath;
            if (null != p)
            {
                if (p.isFile())
                {
                    return Utils.ConvertFromJavaList<Application>(EditorFactory.instance().getEditors(p.getName()), null);
                }
            }
            return new List<Application>();
        }

        private bool View_ValidateNewBrowser()
        {
            return IsMounted();
        }

        private List<KeyValuePair<String, List<String>>> View_GetCopyUrls()
        {
            List<KeyValuePair<String, List<String>>> items = new List<KeyValuePair<String, List<String>>>();
            IList<Path> selected = View.SelectedPaths;
            if (selected.Count == 0)
            {
                items.Add(new KeyValuePair<string, List<String>>(LocaleFactory.localizedString("None"),
                                                                 new List<string>()));
            }
            else
            {
                UrlProvider urlProvider = ((UrlProvider) _session.getFeature(typeof (UrlProvider)));
                if (urlProvider != null)
                {
                    for (int i = 0; i < urlProvider.toUrl(SelectedPath).size(); i++)
                    {
                        DescriptiveUrl descUrl = (DescriptiveUrl) urlProvider.toUrl(SelectedPath).toArray()[i];
                        KeyValuePair<String, List<String>> entry =
                            new KeyValuePair<string, List<string>>(descUrl.getHelp(), new List<string>());
                        items.Add(entry);
                        foreach (Path path in selected)
                        {
                            entry.Value.Add(((DescriptiveUrl) urlProvider.toUrl(path).toArray()[i]).getUrl());
                        }
                    }
                }
                UrlProvider distributionConfiguration =
                    ((UrlProvider) _session.getFeature(typeof (DistributionConfiguration)));
                if (distributionConfiguration != null)
                {
                    for (int i = 0; i < distributionConfiguration.toUrl(SelectedPath).size(); i++)
                    {
                        DescriptiveUrl descUrl =
                            (DescriptiveUrl) distributionConfiguration.toUrl(SelectedPath).toArray()[i];
                        KeyValuePair<String, List<String>> entry =
                            new KeyValuePair<string, List<string>>(descUrl.getHelp(), new List<string>());
                        items.Add(entry);
                        foreach (Path path in selected)
                        {
                            entry.Value.Add(
                                ((DescriptiveUrl) distributionConfiguration.toUrl(path).toArray()[i]).getUrl());
                        }
                    }
                }
            }
            return items;
        }

        private bool IsBrowser()
        {
            return View.CurrentView == BrowserView.File;
        }

        private IList<KeyValuePair<string, List<string>>> View_GetOpenUrls()
        {
            IList<KeyValuePair<String, List<String>>> items = new List<KeyValuePair<String, List<String>>>();
            IList<Path> selected = View.SelectedPaths;
            if (selected.Count == 0)
            {
                items.Add(new KeyValuePair<string, List<String>>(LocaleFactory.localizedString("None"),
                                                                 new List<string>()));
            }
            else
            {
                DescriptiveUrlBag urls =
                    ((UrlProvider) _session.getFeature(typeof (UrlProvider))).toUrl(SelectedPath)
                                                                             .filter(DescriptiveUrl.Type.http,
                                                                                     DescriptiveUrl.Type.cname,
                                                                                     DescriptiveUrl.Type.cdn,
                                                                                     DescriptiveUrl.Type.signed,
                                                                                     DescriptiveUrl.Type.authenticated,
                                                                                     DescriptiveUrl.Type.torrent);
                for (int i = 0; i < urls.size(); i++)
                {
                    DescriptiveUrl descUrl = (DescriptiveUrl) urls.toArray()[i];
                    KeyValuePair<String, List<String>> entry = new KeyValuePair<string, List<string>>(
                        descUrl.getHelp(), new List<string>());
                    items.Add(entry);

                    foreach (Path path in selected)
                    {
                        entry.Value.Add(((DescriptiveUrl) urls.toArray()[i]).getUrl());
                    }
                }
            }
            return items;
        }

        public void UpdateBookmarks()
        {
            View.UpdateBookmarks();
        }

        private bool View_ValidateDuplicateBookmark()
        {
            return _bookmarkModel.Source.allowsEdit() && View.SelectedBookmarks.Count == 1;
        }

        private void View_DuplicateBookmark()
        {
            ToggleView(BrowserView.Bookmark);
            Host duplicate = new HostDictionary().deserialize(View.SelectedBookmark.serialize(SerializerFactory.get()));
            // Make sure a new UUID is asssigned for duplicate
            duplicate.setUuid(null);
            AddBookmark(duplicate);
        }

        private void View_HostModelDropped(ModelDropEventArgs dropargs)
        {
            int sourceIndex = _bookmarkModel.Source.indexOf(dropargs.SourceModels[0]);
            int destIndex = dropargs.DropTargetIndex;
            if (dropargs.DropTargetLocation == DropTargetLocation.BelowItem)
            {
                destIndex++;
            }
            if (dropargs.Effect == DragDropEffects.Copy)
            {
                Host host =
                    new HostDictionary().deserialize(((Host) dropargs.SourceModels[0]).serialize(SerializerFactory.get()));
                host.setUuid(null);
                AddBookmark(host, destIndex);
            }
            if (dropargs.Effect == DragDropEffects.Move)
            {
                if (sourceIndex < destIndex)
                {
                    destIndex--;
                }
                foreach (Host promisedDragBookmark in dropargs.SourceModels)
                {
                    _bookmarkModel.Source.remove(promisedDragBookmark);
                    if (destIndex > _bookmarkModel.Source.size())
                    {
                        _bookmarkModel.Source.add(promisedDragBookmark);
                    }
                    else
                    {
                        _bookmarkModel.Source.add(destIndex, promisedDragBookmark);
                    }
                    //view.selectRowIndexes(NSIndexSet.indexSetWithIndex(row), false);
                    //view.scrollRowToVisible(row);
                }
            }
        }

        private void View_HostModelCanDrop(ModelDropEventArgs args)
        {
            if (!_bookmarkModel.Source.allowsEdit())
            {
                // Do not allow drags for non writable collections
                args.Effect = DragDropEffects.None;
                args.DropTargetLocation = DropTargetLocation.None;
                return;
            }
            switch (args.DropTargetLocation)
            {
                case DropTargetLocation.BelowItem:
                case DropTargetLocation.AboveItem:
                    if (args.SourceModels.Count > 1)
                    {
                        args.Effect = DragDropEffects.Move;
                    }
                    break;
                default:
                    args.Effect = DragDropEffects.None;
                    args.DropTargetLocation = DropTargetLocation.None;
                    return;
            }
        }

        private void View_HostDropped(OlvDropEventArgs e)
        {
            if (e.DataObject is DataObject && ((DataObject) e.DataObject).ContainsFileDropList())
            {
                DataObject data = (DataObject) e.DataObject;

                if (e.DropTargetLocation == DropTargetLocation.Item)
                {
                    IList<TransferItem> roots = new List<TransferItem>();
                    Host host = null;
                    foreach (string filename in data.GetFileDropList())
                    {
                        //check if we received at least one non-duck file
                        if (!".duck".Equals(Utils.GetSafeExtension(filename)))
                        {
                            // The bookmark this file has been dropped onto
                            Host destination = (Host) e.DropTargetItem.RowObject;
                            if (null == host)
                            {
                                host = destination;
                            }
                            Local local = LocalFactory.get(filename);
                            // Upload to the remote host this bookmark points to
                            roots.Add(
                                new TransferItem(
                                    new Path(
                                        new Path(PathNormalizer.normalize(destination.getDefaultPath(), true),
                                                 EnumSet.of(AbstractPath.Type.directory)), local.getName(),
                                        EnumSet.of(AbstractPath.Type.file)), local));
                        }
                    }
                    if (roots.Count > 0)
                    {
                        UploadTransfer q = new UploadTransfer(host, Utils.ConvertToJavaList(roots));
                        // If anything has been added to the queue, then process the queue
                        if (q.getRoots().size() > 0)
                        {
                            TransferController.Instance.StartTransfer(q);
                        }
                    }
                    return;
                }

                if (e.DropTargetLocation == DropTargetLocation.AboveItem)
                {
                    Host destination = (Host) e.DropTargetItem.RowObject;
                    foreach (string file in data.GetFileDropList())
                    {
                        _bookmarkModel.Source.add(_bookmarkModel.Source.indexOf(destination),
                                                  HostReaderFactory.get().read(LocalFactory.get(file)));
                    }
                }
                if (e.DropTargetLocation == DropTargetLocation.BelowItem)
                {
                    Host destination = (Host) e.DropTargetItem.RowObject;
                    foreach (string file in data.GetFileDropList())
                    {
                        _bookmarkModel.Source.add(_bookmarkModel.Source.indexOf(destination) + 1,
                                                  HostReaderFactory.get().read(LocalFactory.get(file)));
                    }
                }
                if (e.DropTargetLocation == DropTargetLocation.Background)
                {
                    foreach (string file in data.GetFileDropList())
                    {
                        _bookmarkModel.Source.add(HostReaderFactory.get().read(LocalFactory.get(file)));
                    }
                }
            }
        }

        private void View_HostCanDrop(OlvDropEventArgs args)
        {
            if (!_bookmarkModel.Source.allowsEdit())
            {
                // Do not allow drags for non writable collections
                args.Effect = DragDropEffects.None;
                args.DropTargetLocation = DropTargetLocation.None;
                return;
            }

            DataObject dataObject = (DataObject) args.DataObject;
            if (dataObject.ContainsFileDropList())
            {
                //check if all files are .duck files
                foreach (string file in dataObject.GetFileDropList())
                {
                    string ext = Utils.GetSafeExtension(file);
                    if (!".duck".Equals(ext))
                    {
                        //if at least one non-duck file we prepare for uploading
                        args.Effect = DragDropEffects.Copy;
                        if (args.DropTargetLocation == DropTargetLocation.Item)
                        {
                            Host destination = (Host) args.DropTargetItem.RowObject;
                            (args.DataObject as DataObject).SetDropDescription((DropImageType) args.Effect,
                                                                               "Upload to %1",
                                                                               BookmarkNameProvider.toString(destination));
                        }
                        args.DropTargetLocation = DropTargetLocation.Item;
                        return;
                    }
                }

                //at least one .duck file
                args.Effect = DragDropEffects.Copy;
                if (args.DropTargetLocation == DropTargetLocation.Item)
                {
                    args.DropTargetLocation = DropTargetLocation.Background;
                }
                return;
            }
            args.Effect = DragDropEffects.None;
        }

        private void View_HostEndDrag(DataObject data)
        {
            RemoveTemporaryFiles(data);
            RemoveTemporaryFilesystemWatcher();
        }

        private string CreateAndWatchTemporaryFile(FileSystemEventHandler del)
        {
            string tfile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            using (File.Create(tfile))
            {
                FileInfo tmpFile = new FileInfo(tfile);
                tmpFile.Attributes |= FileAttributes.Hidden;
            }
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady && d.DriveType != DriveType.CDRom)
                {
                    try
                    {
                        FileSystemWatcher watcher = new FileSystemWatcher(@d.Name, System.IO.Path.GetFileName(tfile));
                        watcher.BeginInit();
                        watcher.IncludeSubdirectories = true;
                        watcher.EnableRaisingEvents = true;
                        watcher.Created += del;
                        watcher.EndInit();
                        _temporaryWatcher.Add(watcher);
                    }
                    catch (Exception e)
                    {
                        Log.info(string.Format("Cannot watch drive {0}", d), e);
                    }
                }
            }
            return tfile;
        }

        private DataObject View_HostDrag(ObjectListView list)
        {
            DataObject data = new DataObject(DataFormats.FileDrop,
                                             new[]
                                                 {
                                                     CreateAndWatchTemporaryFile(
                                                         delegate(object sender, FileSystemEventArgs args)
                                                             {
                                                                 Invoke(delegate
                                                                     {
                                                                         _dropFolder =
                                                                             System.IO.Path.GetDirectoryName(
                                                                                 args.FullPath);
                                                                         foreach (Host host in
                                                                             View.SelectedBookmarks)
                                                                         {
                                                                             string filename =
                                                                                 BookmarkNameProvider.toString(host) +
                                                                                 ".duck";
                                                                             foreach (char c in
                                                                                 System.IO.Path.GetInvalidFileNameChars()
                                                                                 )
                                                                             {
                                                                                 filename =
                                                                                     filename.Replace(c.ToString(),
                                                                                                      String.Empty);
                                                                             }

                                                                             Local file = LocalFactory.get(_dropFolder,
                                                                                                           filename);
                                                                             HostWriterFactory.get().write(host, file);
                                                                         }
                                                                     });
                                                             })
                                                 });
            return data;
        }

        private void View_BrowserModelCanDrop(ModelDropEventArgs args)
        {
            if (IsMounted())
            {
                Path destination;
                switch (args.DropTargetLocation)
                {
                    case DropTargetLocation.Item:
                        destination = (Path) args.DropTargetItem.RowObject;
                        if (!destination.isDirectory())
                        {
                            //dragging over file
                            destination = destination.getParent();
                        }
                        break;
                    case DropTargetLocation.Background:
                        destination = Workdir;
                        break;
                    default:
                        args.Effect = DragDropEffects.None;
                        args.DropTargetLocation = DropTargetLocation.None;
                        return;
                }
                Touch feature = (Touch) _session.getFeature(typeof (Touch));
                if (!feature.isSupported(destination))
                {
                    args.Effect = DragDropEffects.None;
                    args.DropTargetLocation = DropTargetLocation.None;
                    return;
                }
                foreach (Path sourcePath in args.SourceModels)
                {
                    if (args.ListView == args.SourceListView)
                    {
                        // Use drag action from user
                    }
                    else
                    {
                        // If copying between sessions is supported
                        args.Effect = DragDropEffects.Copy;
                    }
                    if (sourcePath.isDirectory() && sourcePath.equals(destination))
                    {
                        // Do not allow dragging onto myself.
                        args.Effect = DragDropEffects.None;
                        args.DropTargetLocation = DropTargetLocation.None;
                        return;
                    }
                    if (sourcePath.isDirectory() && destination.isChild(sourcePath))
                    {
                        // Do not allow dragging a directory into its own containing items
                        args.Effect = DragDropEffects.None;
                        args.DropTargetLocation = DropTargetLocation.None;
                        return;
                    }
                    if (sourcePath.isFile() && sourcePath.getParent().equals(destination))
                    {
                        // Moving file to the same destination makes no sense
                        args.Effect = DragDropEffects.None;
                        args.DropTargetLocation = DropTargetLocation.None;
                        return;
                    }
                }
                if (Workdir == destination)
                {
                    args.DropTargetLocation = DropTargetLocation.Background;
                }
                else
                {
                    args.DropTargetItem = args.ListView.ModelToItem(destination);
                }
            }
        }

        /// <summary>
        /// A file dragged within the browser has been received
        /// </summary>
        /// <param name="dropargs"></param>
        private void View_BrowserModelDropped(ModelDropEventArgs dropargs)
        {
            Path destination;
            switch (dropargs.DropTargetLocation)
            {
                case DropTargetLocation.Item:
                    destination = (Path) dropargs.DropTargetItem.RowObject;
                    break;
                case DropTargetLocation.Background:
                    destination = Workdir;
                    break;
                default:
                    destination = null;
                    break;
            }
            if (null != destination)
            {
                IDictionary<Path, Path> files = new Dictionary<Path, Path>();
                foreach (Path next in dropargs.SourceModels)
                {
                    Path renamed = new Path(destination, next.getName(), next.getType());
                    files.Add(next, renamed);
                }
                if (dropargs.Effect == DragDropEffects.Copy)
                {
                    foreach (BrowserController controller in MainController.Browsers)
                    {
                        // Find source browser
                        if (controller.View.Browser.Equals(dropargs.SourceListView))
                        {
                            controller.transfer(
                                new CopyTransfer(controller.Session.getHost(), Session.getHost(),
                                                 Utils.ConvertToJavaMap(files)), new List<Path>(files.Values), false);
                            break;
                        }
                    }
                }
                if (dropargs.Effect == DragDropEffects.Move)
                {
                    // The file should be renamed
                    RenamePaths(files);
                }
            }
        }

        private void View_Download()
        {
            Download(SelectedPaths, new DownloadDirectoryFinder().find(_session.getHost()));
        }

        private bool View_ValidateRevertFile()
        {
            if (IsMounted() && SelectedPaths.Count == 1)
            {
                return Session.getFeature(typeof (Versioning)) != null;
            }
            return false;
        }

        private void View_RevertFile()
        {
            RevertPaths(SelectedPaths);
        }

        private void RevertPaths(IList<Path> files)
        {
            Background(new RevertAction(this, files));
        }

        private void View_ToggleBookmarks()
        {
            if (View.CurrentView == BrowserView.File)
            {
                View.CurrentView = _lastBookmarkView;
            }
            else
            {
                _lastBookmarkView = View.CurrentView;
                View.CurrentView = BrowserView.File;
            }
        }

        private bool View_ValidateSearchField()
        {
            return IsMounted() || View.CurrentView != BrowserView.File;
        }

        private bool View_ValidatePathsCombobox()
        {
            return IsMounted();
        }

        private void View_ItemsChanged()
        {
            SetStatus();
        }

        private void View_Certificate()
        {
            if (_session is SSLSession)
            {
                SSLSession secured = (SSLSession) _session;
                List certificates = secured.getAcceptedIssuers();
                CertificateStoreFactory.get().display(certificates);
            }
        }

        private void View_ClearHistory()
        {
            HistoryCollection.defaultCollection().clear();
        }

        private List<Host> View_GetBonjourHosts()
        {
            List<Host> b = new List<Host>();
            foreach (Host h in RendezvousCollection.defaultCollection())
            {
                b.Add(h);
            }
            return b;
        }

        private List<Host> View_GetHistory()
        {
            List<Host> b = new List<Host>();
            foreach (Host h in HistoryCollection.defaultCollection())
            {
                b.Add(h);
            }
            return b;
        }

        private List<Host> View_GetBookmarks()
        {
            List<Host> b = new List<Host>();
            foreach (Host h in BookmarkCollection.defaultCollection())
            {
                b.Add(h);
            }
            return b;
        }

        private void PopulateEncodings()
        {
            List<string> list = new List<string>();
            list.AddRange(new DefaultCharsetProvider().availableCharsets());
            View.PopulateEncodings(list);
            View.SelectedEncoding = Preferences.instance().getProperty("browser.charset.encoding");
        }

        private void View_EncodingChanged(object sender, EncodingChangedArgs e)
        {
            string encoding = e.Encoding;
            if (Utils.IsBlank(encoding))
            {
                return;
            }
            View.SelectedEncoding = encoding;
            if (IsMounted())
            {
                if (_session.getEncoding().Equals(encoding))
                {
                    return;
                }
                _session.getHost().setEncoding(encoding);
                Mount(_session.getHost());
            }
        }

        private void View_ConnectBookmark(object sender, ConnectBookmarkArgs connectBookmarkArgs)
        {
            Mount(connectBookmarkArgs.Bookmark);
        }

        private bool View_ValidateConnectBookmark()
        {
            return View.SelectedBookmarks.Count == 1;
        }

        private bool View_ValidateDeleteBookmark()
        {
            return _bookmarkModel.Source.allowsDelete() && View.SelectedBookmarks.Count > 0;
        }

        private bool View_ValidateEditBookmark()
        {
            return _bookmarkModel.Source.allowsEdit() && View.SelectedBookmarks.Count == 1;
        }

        private bool View_ValidateNewBookmark()
        {
            return _bookmarkModel.Source.allowsAdd();
        }

        private void View_ChangeBrowserView(object sender, ChangeBrowserViewArgs e)
        {
            ToggleView(e.View);
        }

        private void View_EditBookmark()
        {
            if (View.SelectedBookmarks.Count == 1)
            {
                BookmarkController.Factory.Create(View.SelectedBookmark).View.Show(View);
            }
        }

        private void View_NewBookmark()
        {
            Host bookmark;
            if (IsMounted())
            {
                Path selected = SelectedPath;
                if (null == selected || !selected.isDirectory())
                {
                    selected = Workdir;
                }
                bookmark = new HostDictionary().deserialize(_session.getHost().serialize(SerializerFactory.get()));
                bookmark.setUuid(null);
                bookmark.setDefaultPath(selected.getAbsolute());
            }
            else
            {
                bookmark =
                    new Host(
                        ProtocolFactory.forName(Preferences.instance().getProperty("connection.protocol.default")),
                        Preferences.instance().getProperty("connection.hostname.default"),
                        Preferences.instance().getInteger("connection.port.default"));
            }
            ToggleView(BrowserView.Bookmark);
            AddBookmark(bookmark);
        }

        public void AddBookmark(Host item)
        {
            AddBookmark(item, -1);
        }

        private void AddBookmark(Host item, int index)
        {
            _bookmarkModel.Filter = null;
            if (index != -1)
            {
                _bookmarkModel.Source.add(index, item);
            }
            else
            {
                _bookmarkModel.Source.add(item);
            }
            View.SelectBookmark(item);
            View.EnsureBookmarkVisible(item);
            BookmarkController.Factory.Create(item).View.Show(View);
        }

        private void View_DeleteBookmark()
        {
            List<Host> selected = View.SelectedBookmarks;
            StringBuilder alertText = new StringBuilder();
            int i = 0;
            foreach (Host host in selected)
            {
                if (i > 0)
                {
                    alertText.Append("\n");
                }
                alertText.Append(Character.toString('\u2022')).Append(" ").Append(BookmarkNameProvider.toString(host));
                i++;
                if (i > 10)
                {
                    break;
                }
            }
            DialogResult result = QuestionBox(LocaleFactory.localizedString("Delete Bookmark"),
                                              LocaleFactory.localizedString(
                                                  "Do you want to delete the selected bookmark?"), alertText.ToString(),
                                              String.Format("{0}", LocaleFactory.localizedString("Delete")), true);
            if (result == DialogResult.OK)
            {
                _bookmarkModel.Source.removeAll(Utils.ConvertToJavaList(selected));
            }
        }

        public override bool ViewShouldClose()
        {
            return Unmount();
        }

        private void View_OpenUrl()
        {
            DescriptiveUrlBag list;
            if (SelectedPaths.Count == 1)
            {
                list = ((UrlProvider) _session.getFeature(typeof (UrlProvider))).toUrl(SelectedPath);
            }
            else
            {
                list = ((UrlProvider) _session.getFeature(typeof (UrlProvider))).toUrl(Workdir);
            }
            if (!list.isEmpty())
            {
                BrowserLauncherFactory.get().open(list.find(DescriptiveUrl.Type.http).getUrl());
            }
        }

        private void View_SearchFieldChanged()
        {
            if (View.CurrentView == BrowserView.File)
            {
                SetPathFilter(View.SearchString);
            }
            else
            {
                SetBookmarkFilter(View.SearchString);
            }
        }

        private void SetBookmarkFilter(string searchString)
        {
            if (Utils.IsBlank(searchString))
            {
                View.SearchString = String.Empty;
                _bookmarkModel.Filter = null;
            }
            else
            {
                _bookmarkModel.Filter = new BookmarkFilter(searchString);
            }
            ReloadBookmarks();
        }

        private bool View_ValidateDisconnect()
        {
            // disconnect/stop button update
            View.ActivityRunning = isActivityRunning();
            if (!IsConnected())
            {
                return isActivityRunning();
            }
            return IsConnected();
        }

        private bool View_ValidateStop()
        {
            return isActivityRunning();
        }

        private bool View_ValidateSendCustomCommand()
        {
            return IsMounted() && _session.getFeature(typeof (Command)) != null;
        }

        private bool View_ValidateFolderInside()
        {
            return IsMounted() && SelectedPaths.Count > 0;
        }

        private bool View_ValidateFolderUp()
        {
            return IsMounted() && !Workdir.isRoot();
        }

        private bool View_ValidateHistoryForward()
        {
            return IsMounted() && _navigation.getForward().size() > 0;
        }

        private bool View_ValidateHistoryBack()
        {
            return IsMounted() && _navigation.getBack().size() > 1;
        }

        private bool View_ValidateGotoFolder()
        {
            return IsMounted();
        }

        private bool View_ValidateRefresh()
        {
            return IsMounted();
        }

        private void View_Disconnect()
        {
            if (isActivityRunning())
            {
                // Remove all pending actions)
                foreach (BackgroundAction action in getActions().toArray(new BackgroundAction[getActions().size()]))
                {
                    action.cancel();
                }
            }
            CallbackDelegate run = delegate
                {
                    if (Preferences.instance().getBoolean("browser.disconnect.bookmarks.show"))
                    {
                        ToggleView(BrowserView.Bookmark);
                    }
                    else
                    {
                        ToggleView(BrowserView.File);
                    }
                };
            Disconnect(run);
        }

        /**
         * Unmount this session
         */

        private void Disconnect(CallbackDelegate runnable)
        {
            InfoController infoController = _inspector;
            if (infoController != null)
            {
                infoController.View.Close();
            }
            if (HasSession())
            {
                Background(new DisconnectAction(this, runnable));
            }
            else
            {
                runnable();
            }
        }

        private void View_SendCustomCommand()
        {
            new CommandController(this, _session).View.ShowDialog();
        }

        private void View_Search()
        {
            View.StartSearch();
        }

        private void View_FolderInside()
        {
            Path selected = SelectedPath;
            if (null == selected)
            {
                return;
            }
            if (selected.isDirectory())
            {
                SetWorkdir(selected);
            }
            else if (selected.isFile() || View.SelectedPaths.Count > 1)
            {
                if (Preferences.instance().getBoolean("browser.doubleclick.edit"))
                {
                    View_EditEvent(null);
                }
                else
                {
                    View_Download();
                }
            }
        }


        public void Download(IList<Path> downloads, Local downloadFolder)
        {
            IList<TransferItem> items = new List<TransferItem>();
            foreach (Path selected in downloads)
            {
                items.Add(new TransferItem(selected, LocalFactory.get(downloadFolder, selected.getName())));
            }
            Transfer q = new DownloadTransfer(_session.getHost(), Utils.ConvertToJavaList(items));
            transfer(q, new List<Path>());
        }

        private void View_GotoFolder()
        {
            GotoController gc = new GotoController(ObjectFactory.GetInstance<IGotoPromptView>(), this);
            gc.Show();
        }

        private void View_RefreshBrowser()
        {
            if (IsMounted())
            {
                _cache.invalidate(Workdir.getReference());
                foreach (Path path in View.VisiblePaths)
                {
                    if (null == path) continue;
                    _cache.invalidate(path.getReference());
                }
                ReloadData(true);
            }
        }

        private bool View_ValidateTextEncoding()
        {
            return IsMounted() && !isActivityRunning();
        }

        private void View_ToggleLogDrawer()
        {
            View.LogDrawerVisible = !View.LogDrawerVisible;
            Preferences.instance().setProperty("browser.transcript.open", View.LogDrawerVisible);
        }

        private void View_ShowHiddenFiles()
        {
            ShowHiddenFiles = !ShowHiddenFiles;
            if (IsMounted())
            {
                ReloadData(true);
            }
        }

        private void View_ToggleToolbar()
        {
            View.ToolbarVisible = !View.ToolbarVisible;
            Preferences.instance().setProperty("browser.toolbar", View.ToolbarVisible);
        }

        private bool View_ValidatePaste()
        {
            return IsBrowser() && IsMounted() && !_pasteboard.isEmpty();
        }

        private void View_Paste()
        {
            IDictionary<Path, Path> files = new Dictionary<Path, Path>();
            Path parent = Workdir;
            for (int i = 0; i < _pasteboard.size(); i++)
            {
                Path next = (Path) _pasteboard.get(i);
                Path renamed = new Path(parent, next.getName(), next.getType());
                files.Add(next, renamed);
            }
            _pasteboard.clear();
            if (_pasteboard.isCut())
            {
                RenamePaths(files);
            }
            if (_pasteboard.isCopy())
            {
                DuplicatePaths(files);
            }
        }

        private bool View_ValidateCopy()
        {
            return IsBrowser() && IsMounted() && SelectedPaths.Count > 0;
        }

        private void View_Copy()
        {
            _pasteboard.clear();
            _pasteboard.setCopy(true);
            foreach (Path p in SelectedPaths)
            {
                // Writing data for private use when the item gets dragged to the transfer queue.
                _pasteboard.add(p);
            }
        }

        private bool View_ValidateCut()
        {
            return IsBrowser() && IsMounted() && SelectedPaths.Count > 0;
        }

        private void View_Cut()
        {
            _pasteboard.clear();
            _pasteboard.setCut(true);
            foreach (Path s in SelectedPaths)
            {
                // Writing data for private use when the item gets dragged to the transfer queue.
                _pasteboard.add(s);
            }
        }

        private void View_ShowPreferences()
        {
            PreferencesController.Instance.View.Show();
        }

        private bool View_ContextMenuEnabled()
        {
            //context menu is always enabled
            return true;
        }

        private void View_Exit()
        {
            MainController.Exit();
        }

        private List<string> View_GetArchives()
        {
            List<string> result = new List<string>();
            Archive[] archives = Archive.getKnownArchives();
            foreach (Archive archive in archives)
            {
                List selected = Utils.ConvertToJavaList(SelectedPaths, null);
                result.Add(archive.getTitle(selected));
            }
            return result;
        }

        private bool View_ValidateExpandArchive()
        {
            if (IsMounted())
            {
                if (_session.getFeature(typeof (Compress)) == null)
                {
                    return false;
                }
                if (SelectedPaths.Count > 0)
                {
                    foreach (Path selected in SelectedPaths)
                    {
                        if (selected.isDirectory())
                        {
                            return false;
                        }
                        if (!Archive.isArchive(selected.getName()))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void View_ExpandArchive()
        {
            List<Path> expanded = new List<Path>();
            foreach (Path selected in SelectedPaths)
            {
                Archive archive = Archive.forName(selected.getName());
                if (null == archive)
                {
                    continue;
                }
                if (CheckOverwrite(Utils.ConvertFromJavaList<Path>(archive.getExpanded(new ArrayList {selected}))))
                {
                    background(new UnarchiveAction(this, archive, selected, expanded));
                }
            }
        }

        private bool View_ValidateCreateArchive()
        {
            if (IsMounted())
            {
                if (_session.getFeature(typeof (Compress)) == null)
                {
                    return false;
                }
                if (SelectedPaths.Count > 0)
                {
                    foreach (Path selected in SelectedPaths)
                    {
                        if (selected.isFile() && Archive.isArchive(selected.getName()))
                        {
                            // At least one file selected is already an archive. No distinct action possible
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void View_CreateArchive(object sender, CreateArchiveEventArgs createArchiveEventArgs)
        {
            Archive archive = Archive.forName(createArchiveEventArgs.ArchiveName);
            IList<Path> selected = SelectedPaths;
            if (CheckOverwrite(new List<Path> {archive.getArchive(Utils.ConvertToJavaList(selected))}))
            {
                background(new CreateArchiveAction(this, archive, selected));
            }
        }

        private bool View_ValidateDelete()
        {
            return IsMounted() && SelectedPaths.Count > 0;
        }

        private bool View_ValidateSynchronize()
        {
            return IsMounted();
        }

        private void View_Synchronize()
        {
            Path selected;
            if (SelectedPaths.Count == 1 && SelectedPath.isDirectory())
            {
                selected = SelectedPath;
            }
            else
            {
                selected = Workdir;
            }
            string folder =
                View.SynchronizeDialog(
                    String.Format(LocaleFactory.localizedString("Synchronize {0} with"), selected.getName()),
                    new UploadDirectoryFinder().find(Session.getHost()), null);
            if (null != folder)
            {
                transfer(new SyncTransfer(_session.getHost(), new TransferItem(selected, LocalFactory.get(folder))));
            }
        }

        private bool View_ValidateUpload()
        {
            return IsMounted() && ((Touch) _session.getFeature(typeof (Touch))).isSupported(Workdir);
        }

        private void View_Upload()
        {
            // Due to the limited functionality of the OpenFileDialog class it is
            // currently not possible to select a folder. May be we should provide
            // a second menu item which allows to select a folder to upload
            string[] paths = View.UploadDialog(null);
            if (null == paths) return;

            Path destination = new UploadTargetFinder(Workdir).find(SelectedPath);
            List downloads = Utils.ConvertToJavaList(paths, delegate(string path)
                {
                    Local local = LocalFactory.get(path);
                    return
                        new TransferItem(
                            new Path(destination, local.getName(),
                                     local.isDirectory()
                                         ? EnumSet.of(AbstractPath.Type.directory)
                                         : EnumSet.of(AbstractPath.Type.file)), local);
                });
            transfer(new UploadTransfer(Session.getHost(), downloads));
        }

        private void View_DownloadTo()
        {
            string folder = View.DownloadToDialog(LocaleFactory.localizedString("Download To…"),
                                                  new DownloadDirectoryFinder().find(Session.getHost()), null);
            if (null != folder)
            {
                IList<TransferItem> downloads = new List<TransferItem>();
                foreach (Path file in SelectedPaths)
                {
                    downloads.Add(new TransferItem(file, LocalFactory.get(LocalFactory.get(folder), file.getName())));
                }
                transfer(new DownloadTransfer(Session.getHost(), Utils.ConvertToJavaList(downloads)), new List<Path>());
            }
        }

        private bool View_ValidateDownloadAs()
        {
            return IsMounted() && SelectedPaths.Count == 1;
        }

        private void View_DownloadAs()
        {
            string filename = View.DownloadAsDialog(new DownloadDirectoryFinder().find(Session.getHost()),
                                                    SelectedPath.getName());
            if (null != filename)
            {
                Path selected = SelectedPath;
                transfer(new DownloadTransfer(Session.getHost(), selected, LocalFactory.get(filename)), new List<Path>());
            }
        }

        private bool View_ValidateDownload()
        {
            return IsMounted() && SelectedPaths.Count > 0;
        }

        private bool View_ValidateShowInspector()
        {
            return IsMounted() && SelectedPaths.Count > 0;
        }

        private bool View_ValidateOpenWebUrl()
        {
            return IsMounted();
        }

        private bool View_ValidateEditWith()
        {
            if (IsMounted() && SelectedPaths.Count > 0)
            {
                foreach (Path selected in SelectedPaths)
                {
                    if (!IsEditable(selected))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <param name="selected"></param>
        /// <returns>True if the selected path is editable (not a directory)</returns>
        private bool IsEditable(Path selected)
        {
            if (Session.getHost().getCredentials().isAnonymousLogin())
            {
                return false;
            }
            return selected.isFile();
        }

        private bool View_ValidateDuplicateFile()
        {
            return IsMounted() && SelectedPaths.Count == 1;
        }

        private bool View_ValidateRenameFile()
        {
            if (IsMounted() && SelectedPaths.Count == 1)
            {
                if (null == SelectedPath)
                {
                    return false;
                }
                return ((Move) Session.getFeature(typeof (Move))).isSupported(SelectedPath);
            }
            return false;
        }

        private bool View_ValidateNewFile()
        {
            return IsMounted() && ((Touch) _session.getFeature(typeof (Touch))).isSupported(Workdir);
        }

        private void View_NewDownload()
        {
            //todo implement
            return;
            throw new NotImplementedException();
        }

        private void View_OpenConnection()
        {
            ConnectionController c = ConnectionController.Instance(this);
            DialogResult result = c.View.ShowDialog(View);
            if (result == DialogResult.OK)
            {
                Mount(c.ConfiguredHost);
            }
        }

        private bool View_ValidateNewFolder()
        {
            return IsMounted();
        }

        private void View_DuplicateFile()
        {
            DuplicateFileController dc =
                new DuplicateFileController(ObjectFactory.GetInstance<IDuplicateFilePromptView>(), this);
            dc.Show();
        }

        private void View_NewFile()
        {
            CreateFileController fc = new CreateFileController(ObjectFactory.GetInstance<ICreateFilePromptView>(), this);
            fc.Show();
        }

        private void View_Delete()
        {
            DeletePaths(SelectedPaths);
        }

        private void View_NewFolder()
        {
            Location feature = (Location) _session.getFeature(typeof (Location));
            FolderController fc = new FolderController(ObjectFactory.GetInstance<INewFolderPromptView>(), this,
                                                       feature != null
                                                           ? (IList<Location.Name>)
                                                             Utils.ConvertFromJavaList<Location.Name>(
                                                                 feature.getLocations())
                                                           : new List<Location.Name>());
            fc.Show();
        }

        private bool View_RenameFile(Path path, string newName)
        {
            if (!String.IsNullOrEmpty(newName) && !newName.Equals(path.getName()))
            {
                Path renamed = new Path(path.getParent(), newName, path.getType());
                RenamePath(path, renamed);
            }
            return false;
        }

        private DataObject View_BrowserDrag(ObjectListView listView)
        {
            DataObject data = new DataObject(DataFormats.FileDrop,
                                             new[]
                                                 {
                                                     CreateAndWatchTemporaryFile(
                                                         delegate(object sender, FileSystemEventArgs args)
                                                             {
                                                                 _dropFolder =
                                                                     System.IO.Path.GetDirectoryName(args.FullPath);
                                                                 Invoke(
                                                                     delegate
                                                                         {
                                                                             Download(SelectedPaths,
                                                                                      LocalFactory.get(_dropFolder));
                                                                         });
                                                             })
                                                 });
            return data;
        }

        private void RemoveTemporaryFilesystemWatcher()
        {
            BeginInvoke(delegate
                {
                    foreach (FileSystemWatcher watcher in _temporaryWatcher)
                    {
                        watcher.Dispose();
                    }
                    _temporaryWatcher.Clear();
                });
        }

        private void RemoveTemporaryFiles(DataObject data)
        {
            if (data.ContainsFileDropList())
            {
                foreach (string tmpFile in data.GetFileDropList())
                {
                    try
                    {
                        if (File.Exists(tmpFile))
                        {
                            File.Delete(tmpFile);
                        }
                        if (null != _dropFolder)
                        {
                            string tmpDestFile = System.IO.Path.Combine(_dropFolder, System.IO.Path.GetFileName(tmpFile));
                            if (File.Exists(tmpDestFile))
                            {
                                File.Delete(tmpDestFile);
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        Log.error("Could not remove temporary files.", e);
                    }
                }
            }
        }

        private void View_BrowserEndDrag(DataObject data)
        {
            RemoveTemporaryFiles(data);
            RemoveTemporaryFilesystemWatcher();
        }

        private void View_BrowserDropped(OlvDropEventArgs e)
        {
            if (IsMounted() && e.DataObject is DataObject && ((DataObject) e.DataObject).ContainsFileDropList())
            {
                Path destination;
                switch (e.DropTargetLocation)
                {
                    case DropTargetLocation.Item:
                        destination = (Path) e.DropTargetItem.RowObject;
                        break;
                    case DropTargetLocation.Background:
                        destination = Workdir;
                        break;
                    default:
                        destination = null;
                        break;
                }

                StringCollection dropList = (e.DataObject as DataObject).GetFileDropList();
                if (dropList.Count > 0)
                {
                    IList<TransferItem> roots = new List<TransferItem>();
                    foreach (string file in dropList)
                    {
                        Local local = LocalFactory.get(file);
                        roots.Add(
                            new TransferItem(
                                new Path(destination, local.getName(),
                                         local.isDirectory()
                                             ? EnumSet.of(AbstractPath.Type.directory)
                                             : EnumSet.of(AbstractPath.Type.file)), local));
                    }
                    UploadDroppedPath(roots, destination);
                }
            }
        }

        public void UploadDroppedPath(IList<TransferItem> roots, Path destination)
        {
            if (IsMounted())
            {
                UploadTransfer q = new UploadTransfer(_session.getHost(), Utils.ConvertToJavaList(roots));
                if (q.getRoots().size() > 0)
                {
                    transfer(q);
                }
            }
        }

        /// <summary>
        /// Check if we accept drag operation from an external program
        /// </summary>
        /// <param name="args"></param>
        private void View_BrowserCanDrop(OlvDropEventArgs args)
        {
            Log.trace("Entering View_BrowserCanDrop with " + args.Effect);
            if (IsMounted() && !(args.DataObject is OLVDataObject))
            {
                if (args.DataObject is DataObject && ((DataObject) args.DataObject).ContainsFileDropList())
                {
                    Path destination;
                    switch (args.DropTargetLocation)
                    {
                        case DropTargetLocation.Item:
                            destination = (Path) args.DropTargetItem.RowObject;
                            if (!destination.isDirectory())
                            {
                                //dragging over file
                                destination = destination.getParent();
                            }
                            break;
                        case DropTargetLocation.Background:
                            destination = Workdir;
                            break;
                        default:
                            args.Effect = DragDropEffects.None;
                            args.DropTargetLocation = DropTargetLocation.None;
                            return;
                    }
                    Touch feature = (Touch) _session.getFeature(typeof (Touch));
                    if (!feature.isSupported(destination))
                    {
                        Log.trace("Session does not allow file creation");
                        args.Effect = DragDropEffects.None;
                        args.DropTargetLocation = DropTargetLocation.None;
                        return;
                    }
                    Log.trace("Setting effect to copy");
                    args.Effect = DragDropEffects.Copy;
                    if (Workdir == destination)
                    {
                        args.DropTargetLocation = DropTargetLocation.Background;
                    }
                    else
                    {
                        args.DropTargetItem = args.ListView.ModelToItem(destination);
                    }
                    (args.DataObject as DataObject).SetDropDescription((DropImageType) args.Effect, "Copy to %1",
                                                                       destination.getName());
                }
            }
        }

        private void View_ShowTransfers()
        {
            TransferController.Instance.View.Show();
        }

        private void View_ShowInspector()
        {
            IList<Path> selected = SelectedPaths;
            if (selected.Count > 0)
            {
                if (Preferences.instance().getBoolean("browser.info.inspector"))
                {
                    if (null == _inspector || _inspector.View.IsDisposed)
                    {
                        _inspector = InfoController.Factory.Create(this, selected);
                    }
                    else
                    {
                        _inspector.Files = selected;
                    }
                    _inspector.View.Show(View);
                }
                else
                {
                    InfoController c = InfoController.Factory.Create(this, selected);
                    c.View.Show(View);
                }
            }
        }

        private void View_EditEvent(string exe)
        {
            foreach (Path selected in SelectedPaths)
            {
                Editor editor;
                if (Utils.IsBlank(exe))
                {
                    editor = EditorFactory.instance().create(this, _session, selected);
                }
                else
                {
                    editor = EditorFactory.instance().create(this, _session, new Application(exe, null), selected);
                }
                editor.open(new DisabledApplicationQuitCallback());
            }
        }

        private void UpdateEditIcon()
        {
            Path selected = SelectedPath;
            if (null != selected)
            {
                if (IsEditable(selected))
                {
                    Application app = EditorFactory.instance().getEditor(selected.getName());
                    string editCommand = app != null ? app.getIdentifier() : null;
                    if (Utils.IsNotBlank(editCommand))
                    {
                        View.EditIcon =
                            IconCache.Instance.GetFileIconFromExecutable(
                                WindowsApplicationLauncher.GetExecutableCommand(editCommand), IconCache.IconSize.Large)
                                     .ToBitmap();
                        return;
                    }
                }
            }
            View.EditIcon = IconCache.Instance.IconForName("pencil", 32);
        }

        private void UpdateOpenIcon()
        {
            View.OpenIcon = IconCache.Instance.GetDefaultBrowserIcon();
        }

        private void View_BrowserSelectionChanged()
        {
            UpdateEditIcon();

            // update inspector content if available
            IList<Path> selectedPaths = SelectedPaths;

            if (Preferences.instance().getBoolean("browser.info.inspector"))
            {
                if (_inspector != null && _inspector.Visible)
                {
                    if (selectedPaths.Count > 0)
                    {
                        _inspector.Files = selectedPaths;
                    }
                }
            }
        }

        private void View_PathSelectionChanged()
        {
            string selected = View.SelectedComboboxPath;
            if (selected != null)
            {
                Path workdir = Workdir;
                Path p = workdir;
                while (!p.getAbsolute().Equals(selected))
                {
                    p = p.getParent();
                }
                SetWorkdir(p);
                if (workdir.getParent().equals(p))
                {
                    SetWorkdir(p, workdir);
                }
                else
                {
                    SetWorkdir(p);
                }
            }
        }

        private void View_FolderUp()
        {
            Path previous = Workdir;
            SetWorkdir(previous.getParent(), previous);
        }

        private void View_HistoryBack()
        {
            Path selected = _navigation.back();
            if (selected != null)
            {
                Path previous = Workdir;
                if (previous.getParent().equals(selected))
                {
                    SetWorkdir(selected, previous);
                }
                else
                {
                    SetWorkdir(selected);
                }
            }
        }

        private void View_HistoryForward()
        {
            Path selected = _navigation.forward();
            if (selected != null)
            {
                SetWorkdir(selected);
            }
        }

        private void View_BrowserDoubleClicked()
        {
            View_FolderInside();
        }

        private void View_QuickConnect()
        {
            if (string.IsNullOrEmpty(View.QuickConnectValue))
            {
                return;
            }
            string input = View.QuickConnectValue.Trim();

            // First look for equivalent bookmarks
            BookmarkCollection bookmarkCollection = BookmarkCollection.defaultCollection();
            foreach (Host host in bookmarkCollection)
            {
                if (BookmarkNameProvider.toString(host).Equals(input))
                {
                    Mount(host);
                    return;
                }
            }
            Mount(HostParser.parse(input));
        }

        /// <summary>
        /// Open a new browser with the current selected folder as the working directory
        /// </summary>
        private void View_NewBrowser(object sender, NewBrowserEventArgs newBrowserEventArgs)
        {
            if (newBrowserEventArgs.SelectedAsWorkingDir)
            {
                Path selected = SelectedPath;
                if (null == selected || !selected.isDirectory())
                {
                    selected = Workdir;
                }
                BrowserController c = MainController.NewBrowser(true);

                Host host = new HostDictionary().deserialize(Session.getHost().serialize(SerializerFactory.get()));
                host.setDefaultPath(selected.getAbsolute());
                c.Mount(host);
            }
            else
            {
                BrowserController c = MainController.NewBrowser(true);
                MainController.OpenDefaultBookmark(c);
            }
        }

        protected void transfer(Transfer transfer)
        {
            this.transfer(transfer, Utils.ConvertFromJavaList(transfer.getRoots(), delegate(object o)
                {
                    TransferItem item = (TransferItem) o;
                    return item.remote;
                }));
        }

        /// <summary>
        /// Transfers the files either using the queue or using
        /// the browser session if #connection.pool.max is 1
        /// </summary>
        /// <param name="transfer"></param>
        protected void transfer(Transfer transfer, IList<Path> selected)
        {
            this.transfer(transfer, selected, Session.getMaxConnections() == 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="destination"></param>
        /// <param name="useBrowserConnection"></param>
        public void transfer(Transfer transfer, IList<Path> selected, bool browser)
        {
            TransferCallback callback = new ReloadTransferCallback(this, selected);
            if (browser)
            {
                Background(new CallbackTransferBackgroundAction(callback, this, new ProgressTransferAdapter(this), this,
                                                                this, transfer, new TransferOptions()));
            }
            else
            {
                TransferController.Instance.StartTransfer(transfer, new TransferOptions(), callback);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>true if a connection is being opened or is already initialized</returns>
        public bool HasSession()
        {
            return _session != null;
        }

        public bool IsMounted()
        {
            return HasSession() && Workdir != null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preserveSelection">All selected files should be reselected after reloading the view</param>
        public void ReloadData(bool preserveSelection)
        {
            if (preserveSelection)
            {
                //Remember the previously selected paths
                ReloadData(SelectedPaths);
            }
            else
            {
                ReloadData(new List<Path>());
            }
        }

        public void RefreshParentPath(Path changed)
        {
            RefreshParentPaths(new Collection<Path> {changed});
        }

        public void RefreshParentPaths(IList<Path> changed)
        {
            RefreshParentPaths(changed, new List<Path>());
        }

        public Referenceable Lookup(PathReference reference)
        {
            if (IsMounted())
            {
                return _cache.lookup(reference);
            }
            return null;
        }

        public override void start(BackgroundAction action)
        {
            Invoke(delegate { View.StartActivityAnimation(); });
        }

        public override void stop(BackgroundAction action)
        {
            Invoke(delegate { View.StopActivityAnimation(); });
        }

        public void RefreshParentPaths(IList<Path> changed, IList<Path> selected)
        {
            bool rootRefreshed = false; //prevent multiple root updates
            foreach (Path path in changed)
            {
                _cache.invalidate(path.getParent().getReference());
                if (Workdir.equals(path.getParent()))
                {
                    if (rootRefreshed)
                    {
                        continue;
                    }
                    View.SetBrowserModel(_browserModel.ChildrenGetter(Workdir));
                    rootRefreshed = true;
                }
                else
                {
                    View.RefreshBrowserObject(path.getParent());
                }
            }
            SelectedPaths = selected;
        }

        public void ReloadData(Path directory, bool preserveSelection)
        {
            if (Workdir.equals(directory))
            {
                ReloadData(true);
            }
            else
            {
                View.RefreshBrowserObject(directory);
            }
        }

        protected void ReloadData(IList<Path> selected)
        {
            if (null != Workdir)
            {
                IEnumerable<Path> children = _browserModel.ChildrenGetter(Workdir);
                //clear selection before resetting model. Otherwise we have weird selection effects.
                SelectedPaths = new List<Path>();
                int savedIndex = View.TopItemIndex;
                View.BeginBrowserUpdate();
                View.SetBrowserModel(null); // #7670
                View.SetBrowserModel(children);
                View.TopItemIndex = savedIndex;
                SelectedPaths = selected;
                List<Path> toUpdate = new List<Path>();
                foreach (Path path in View.VisiblePaths)
                {
                    if (path.isDirectory())
                    {
                        toUpdate.Add(path);
                    }
                }
                View.RefreshBrowserObjects(toUpdate);
                View.EndBrowserUpdate();
            }
            else
            {
                View.SetBrowserModel(null);
            }
            SelectedPaths = selected;
            SetStatus();
        }

        public void SetWorkdir(Path directory)
        {
            SetWorkdir(directory, new List<Path>());
        }

        public void SetWorkdir(Path directory, Path selected)
        {
            SetWorkdir(directory, new List<Path> {selected});
        }

        /// <summary>
        /// Sets the current working directory. This will udpate the path selection dropdown button
        /// and also add this path to the browsing history. If the path cannot be a working directory (e.g. permission
        /// issues trying to enter the directory), reloading the browser view is canceled and the working directory
        /// not changed.
        /// </summary>
        /// <param name="directory">The new working directory to display or null to detach any working directory from the browser</param>
        /// <param name="selected"></param>
        public void SetWorkdir(Path directory, List<Path> selected)
        {
            Workdir = directory;
            // Change to last selected browser view
            ReloadData(Workdir != null ? selected : new List<Path>());
            SetNavigation(IsMounted());
        }

        private void SetNavigation(bool enabled)
        {
            View.SearchEnabled = enabled;
            if (!enabled)
            {
                View.SearchString = String.Empty;
            }
            List<string> paths = new List<string>();
            if (enabled)
            {
                // Update the current working directory
                _navigation.add(Workdir);
                Path p = Workdir;
                do
                {
                    paths.Add(p.getAbsolute());
                    p = p.getParent();
                } while (!p.isRoot());
                View.PopulatePaths(paths);
            }
            View.ComboboxPathEnabled = enabled;
            View.HistoryBackEnabled = enabled && _navigation.getBack().size() > 1;
            View.HistoryForwardEnabled = enabled && _navigation.getForward().size() > 0;
            View.ParentPathEnabled = enabled && !Workdir.isRoot();
        }

        public void RefreshObject(Path path, bool preserveSelection)
        {
            if (preserveSelection)
            {
                RefreshObject(path, View.SelectedPaths);
            }
            else
            {
                RefreshObject(path, new List<Path>());
            }
        }

        public void RefreshObject(Path path, IList<Path> selected)
        {
            if (Workdir.Equals(path))
            {
                View.SetBrowserModel(_browserModel.ChildrenGetter(path));
            }
            else
            {
                if (!path.isDirectory())
                {
                    View.RefreshBrowserObject(path.getParent());
                }
                else
                {
                    View.RefreshBrowserObject(path);
                }
            }
            SelectedPaths = selected;
            SetStatus();
        }

        public void Mount(Host host)
        {
            if (Log.isDebugEnabled())
            {
                Log.debug(string.Format("Mount session for {0}", host));
            }
            CallbackDelegate callbackDelegate = delegate
                {
                    // The browser has no session, we are allowed to proceed
                    // Initialize the browser with the new session attaching all listeners
                    Session session = Init(host);
                    background(new MountAction(this, session, host));
                };
            Unmount(callbackDelegate);
        }

        /// <summary>
        /// Initializes a session for the passed host. Setting up the listeners and adding any callback
        /// controllers needed for login, trust management and hostkey verification.
        /// </summary>
        /// <param name="host"></param>
        /// <returns>A session object bound to this browser controller</returns>
        private Session Init(Host host)
        {
            _session = SessionFactory.create(host);
            SetWorkdir(null);
            View.SelectedEncoding = _session.getEncoding();
            View.ClearTranscript();
            _navigation.clear();
            _pasteboard = PathPasteboardFactory.getPasteboard(_session);
            return _session;
        }

        // some simple caching as _session.isConnected() throws a ConnectionCanceledException if not connected

        /// <summary>
        ///
        /// </summary>
        /// <returns>true if mounted and the connection to the server is alive</returns>
        public bool IsConnected()
        {
            if (IsMounted())
            {
                return _session.isConnected();
            }
            return false;
        }

        public static bool ApplicationShouldTerminate()
        {
            // Determine if there are any open connections
            foreach (BrowserController controller in new List<BrowserController>(MainController.Browsers))
            {
                BrowserController c = controller;
                if (!controller.Unmount(delegate(DialogResult result)
                    {
                        if (DialogResult.OK == result)
                        {
                            c.View.Dispose();
                            return true;
                        }
                        return false;
                    }, delegate { }))
                {
                    return false; // Disconnect cancelled
                }
            }
            return true;
        }

        public bool Unmount()
        {
            return Unmount(() => { });
        }

        public bool Unmount(CallbackDelegate disconnected)
        {
            return Unmount(result =>
                {
                    if (DialogResult.OK == result)
                    {
                        UnmountImpl(disconnected);
                        return true;
                    }
                    // No unmount yet
                    return false;
                }, disconnected);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unmountImpl"></param>
        /// <param name="disconnected"></param>
        /// <returns>True if the unmount process is in progress or has been finished, false if cancelled</returns>
        public bool Unmount(DialogCallbackDelegate unmountImpl, CallbackDelegate disconnected)
        {
            if (IsConnected() || isActivityRunning())
            {
                if (Preferences.instance().getBoolean("browser.disconnect.confirm"))
                {
                    DialogResult result = CommandBox(LocaleFactory.localizedString("Disconnect"),
                                                     String.Format(
                                                         LocaleFactory.localizedString("Disconnect from {0}"),
                                                         _session.getHost().getHostname()),
                                                     LocaleFactory.localizedString("The connection will be closed."),
                                                     String.Format("{0}", LocaleFactory.localizedString("Disconnect")),
                                                     true,
                                                     LocaleFactory.localizedString("Don't ask again", "Configuration"),
                                                     SysIcons.Question, delegate(int option, bool verificationChecked)
                                                         {
                                                             if (verificationChecked)
                                                             {
                                                                 // Never show again.
                                                                 Preferences.instance()
                                                                            .setProperty("browser.disconnect.confirm",
                                                                                         false);
                                                             }
                                                             switch (option)
                                                             {
                                                                 case 0: // Disconnect
                                                                     unmountImpl(DialogResult.OK);
                                                                     break;
                                                             }
                                                         });
                    return DialogResult.OK == result;
                }
            }
            UnmountImpl(disconnected);
            // Unmount succeeded
            return true;
        }

        private void UnmountImpl(CallbackDelegate disconnected)
        {
            CallbackDelegate run = delegate
                {
                    _session = null;
                    _cache.clear();
                    View.WindowTitle = Preferences.instance().getProperty("application.name");
                    disconnected();
                };

            Disconnect(run);
        }

        public void SetStatus()
        {
            BackgroundAction current = getActions().getCurrent();
            message(null != current ? current.getActivity() : null);
        }

        public void SetStatus(string label)
        {
            View.StatusLabel = label;
        }

        private void PopulateQuickConnect()
        {
            List<string> nicknames = new List<string>();
            foreach (Host host in _bookmarkCollection)
            {
                nicknames.Add(BookmarkNameProvider.toString(host));
            }
            View.PopulateQuickConnect(nicknames);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The existing file</param>
        /// <param name="renamed">The renamed file</param>
        protected internal void RenamePath(Path path, Path renamed)
        {
            RenamePaths(new Dictionary<Path, Path> {{path, renamed}});
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="selected">
        /// A dictionary with the original files as the key and
        /// the destination files as the value
        /// </param>
        protected internal void RenamePaths(IDictionary<Path, Path> selected)
        {
            if (CheckMove(selected))
            {
                List<Path> changed = new List<Path>();
                changed.AddRange(selected.Keys);
                changed.AddRange(selected.Values);
                MoveAction move = new MoveAction(this, Utils.ConvertToJavaMap(selected), changed);
                Background(move);
            }
        }

        /// <summary>
        /// Displays a warning dialog about files to be moved
        /// </summary>
        /// <param name="selected">The files to check for existence</param>
        /// <param name="action"></param>
        private bool CheckMove(IDictionary<Path, Path> selected)
        {
            if (Preferences.instance().getBoolean("browser.move.confirm"))
            {
                StringBuilder alertText =
                    new StringBuilder(LocaleFactory.localizedString("Do you want to move the selected files?"));

                StringBuilder content = new StringBuilder();
                int i = 0;
                bool rename = false;
                IEnumerator<KeyValuePair<Path, Path>> enumerator = null;
                for (enumerator = selected.GetEnumerator(); i < 10 && enumerator.MoveNext();)
                {
                    KeyValuePair<Path, Path> next = enumerator.Current;
                    if (next.Key.getParent().equals(next.Value.getParent()))
                    {
                        rename = true;
                    }
                    // u2022 = Bullet
                    content.Append("\n" + Character.toString('\u2022') + " " + next.Key.getName());
                    i++;
                }
                if (enumerator.MoveNext())
                {
                    content.Append("\n" + Character.toString('\u2022') + " ...)");
                }
                bool result = false;
                CommandBox(rename ? LocaleFactory.localizedString("Rename") : LocaleFactory.localizedString("Move"),
                           alertText.ToString(), content.ToString(),
                           String.Format("{0}",
                                         rename
                                             ? LocaleFactory.localizedString("Rename")
                                             : LocaleFactory.localizedString("Move")), true,
                           LocaleFactory.localizedString("Don't ask again", "Configuration"), SysIcons.Question,
                           delegate(int option, bool verificationChecked)
                               {
                                   if (verificationChecked)
                                   {
                                       // Never show again.
                                       Preferences.instance().setProperty("browser.move.confirm", false);
                                   }
                                   if (option == 0)
                                   {
                                       result = CheckOverwrite(selected.Values);
                                   }
                               });
                return result;
            }
            return CheckOverwrite(selected.Values);
        }

        /// <summary>
        /// Recursively deletes the files
        /// </summary>
        /// <param name="selected">The files selected in the browser to delete</param>
        public void DeletePaths(ICollection<Path> selected)
        {
            ICollection<Path> normalized =
                Utils.ConvertFromJavaList<Path>(PathNormalizer.normalize(Utils.ConvertToJavaList(selected)));
            if (normalized.Count == 0)
            {
                return;
            }

            StringBuilder alertText =
                new StringBuilder(
                    LocaleFactory.localizedString("Really delete the following files? This cannot be undone."));

            StringBuilder content = new StringBuilder();
            int i = 0;
            IEnumerator<Path> enumerator;
            for (enumerator = selected.GetEnumerator(); i < 10 && enumerator.MoveNext();)
            {
                Path item = enumerator.Current;
                if (i > 0) content.AppendLine();
                // u2022 = Bullet
                content.Append(Character.toString('\u2022') + " " + item.getName());
                i++;
            }
            if (enumerator.MoveNext())
            {
                content.Append("\n" + Character.toString('\u2022') + " ...)");
            }
            DialogResult r = QuestionBox(LocaleFactory.localizedString("Delete"), alertText.ToString(),
                                         content.ToString(),
                                         String.Format("{0}", LocaleFactory.localizedString("Delete")), true);
            if (r == DialogResult.OK)
            {
                DeletePathsImpl(normalized);
            }
        }

        private void DeletePathsImpl(ICollection<Path> files)
        {
            background(new DeleteAction(this, LoginCallbackFactory.get(this), Utils.ConvertToJavaList(files)));
        }

        public void SetPathFilter(string searchString)
        {
            if (Utils.IsBlank(searchString))
            {
                View.SearchString = String.Empty;
                // Revert to the last used default filter
                if (ShowHiddenFiles)
                {
                    FilenameFilter = new NullPathFilter();
                }
                else
                {
                    FilenameFilter = new RegexFilter();
                }
            }
            else
            {
                // Setting up a custom filter for the directory listing
                FilenameFilter = new CustomPathFilter(searchString, _cache);
            }
            ReloadData(true);
        }

        /// <summary>
        /// Displays a warning dialog about already existing files
        /// </summary>
        /// <param name="selected">The files to check for existance</param>
        private bool CheckOverwrite(ICollection<Path> selected)
        {
            StringBuilder alertText =
                new StringBuilder(
                    LocaleFactory.localizedString(
                        "A file with the same name already exists. Do you want to replace the existing file?"));

            StringBuilder content = new StringBuilder();
            int i = 0;
            IEnumerator<Path> enumerator = null;
            bool shouldWarn = false;
            for (enumerator = selected.GetEnumerator(); enumerator.MoveNext();)
            {
                Path item = enumerator.Current;
                if (Lookup(item.getReference()) != null)
                {
                    if (i < 10)
                    {
                        // u2022 = Bullet
                        content.Append("\n" + Character.toString('\u2022') + " " + item.getName());
                    }
                    shouldWarn = true;
                }
                i++;
            }
            if (i >= 10)
            {
                content.Append("\n" + Character.toString('\u2022') + " ...)");
            }
            if (shouldWarn)
            {
                DialogResult r = QuestionBox(LocaleFactory.localizedString("Overwrite"), alertText.ToString(),
                                             content.ToString(),
                                             String.Format("{0}", LocaleFactory.localizedString("Overwrite")), true);
                return r == DialogResult.OK;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source">The original file to duplicate</param>
        /// <param name="destination">The destination of the duplicated file</param>
        protected internal void DuplicatePath(Path source, Path destination)
        {
            DuplicatePaths(new Dictionary<Path, Path> {{source, destination}});
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="selected">A dictionary with the original files as the key and the destination files as the value</param>
        ///<param name="browser"></param>
        protected internal void DuplicatePaths(IDictionary<Path, Path> selected)
        {
            if (CheckOverwrite(selected.Values))
            {
                CopyTransfer copy = new CopyTransfer(_session.getHost(), _session.getHost(),
                                                     Utils.ConvertToJavaMap(selected));
                List<Path> changed = new List<Path>();
                changed.AddRange(selected.Values);
                transfer(copy, changed, true);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="view">The view to show</param>
        public void ToggleView(BrowserView view)
        {
            Log.debug("ToggleView:" + view);
            if (View.CurrentView == view) return;

            SetBookmarkFilter(null);
            switch (view)
            {
                case BrowserView.File:
                    View.CurrentView = BrowserView.File;
                    SetPathFilter(null);
                    ReloadData(true);
                    break;
                case BrowserView.Bookmark:
                    View.CurrentView = BrowserView.Bookmark;
                    _bookmarkModel.Source = BookmarkCollection.defaultCollection();
                    ReloadBookmarks();
                    SelectHost();
                    break;
                case BrowserView.History:
                    View.CurrentView = BrowserView.History;
                    _bookmarkModel.Source = HistoryCollection.defaultCollection();
                    ReloadBookmarks();
                    SelectHost();
                    break;
                case BrowserView.Bonjour:
                    View.CurrentView = BrowserView.Bonjour;
                    _bookmarkModel.Source = RendezvousCollection.defaultCollection();
                    ReloadBookmarks();
                    SelectHost();
                    break;
            }
        }

        private void SelectHost()
        {
            if (IsMounted())
            {
                View.SelectBookmark(Session.getHost());
            }
        }

        /// <summary>
        /// Reload bookmarks table from the currently selected model
        /// </summary>
        public void ReloadBookmarks()
        {
            ReloadBookmarks(null);
        }

        /// <summary>
        /// Reload bookmarks table from the currently selected model
        /// </summary>
        public void ReloadBookmarks(Host selected)
        {
            //Note: expensive for a big bookmark list (might need a refactoring)
            View.SetBookmarkModel(_bookmarkModel.Source, selected);
            SetStatus();
        }

        private class BookmarkFilter : HostFilter
        {
            private readonly string _searchString;

            public BookmarkFilter(String searchString)
            {
                _searchString = searchString;
            }

            public bool accept(Host host)
            {
                return BookmarkNameProvider.toString(host).ToLower().Contains(_searchString.ToLower()) ||
                       (null == host.getComment()
                            ? false
                            : host.getComment().ToLower().Contains(_searchString.ToLower())) ||
                       (null == host.getCredentials().getUsername()
                            ? false
                            : host.getCredentials().getUsername().ToLower().Contains(_searchString.ToLower())) ||
                       host.getHostname().ToLower().Contains(_searchString.ToLower());
            }
        }

        private class CallbackTransferBackgroundAction : TransferBackgroundAction
        {
            private readonly TransferCallback _callback;
            private readonly Transfer _transfer;

            public CallbackTransferBackgroundAction(TransferCallback callback, BrowserController controller,
                                                    TransferListener transferListener, ProgressListener progressListener,
                                                    TranscriptListener transcriptListener, Transfer transfer,
                                                    TransferOptions options)
                : base(
                    controller, controller._session, transferListener, progressListener, transcriptListener, transfer,
                    options)
            {
                _callback = callback;
                _transfer = transfer;
            }

            public override void finish()
            {
                if (_transfer.isComplete())
                {
                    _callback.complete(_transfer);
                }
            }
        }

        private class CreateArchiveAction : BrowserControllerBackgroundAction
        {
            private readonly Archive _archive;
            private readonly IList<Path> _selected;
            private readonly List _selectedJava;

            public CreateArchiveAction(BrowserController controller, Archive archive, IList<Path> selected)
                : base(controller)
            {
                _archive = archive;
                _selectedJava = Utils.ConvertToJavaList(selected);
                _selected = selected;
            }

            public override object run()
            {
                ((Compress) BrowserController._session.getFeature(typeof (Compress))).archive(_archive,
                                                                                              BrowserController.Workdir,
                                                                                              _selectedJava,
                                                                                              BrowserController,
                                                                                              BrowserController);
                return true;
            }

            public override string getActivity()
            {
                return _archive.getCompressCommand(BrowserController.Workdir, _selectedJava);
            }

            public override void cleanup()
            {
                base.cleanup();
                BrowserController.RefreshParentPaths(_selected, new List<Path> {_archive.getArchive(_selectedJava)});
            }
        }

        private class CustomPathFilter : SearchFilter, IModelFilter
        {
            public CustomPathFilter(String searchString, Cache cache) : base(cache, searchString)
            {
            }

            public bool Filter(object modelObject)
            {
                return accept(modelObject);
            }
        }

        private class DeleteAction : WorkerBackgroundAction
        {
            public DeleteAction(BrowserController controller, LoginCallback prompt, List files)
                : base(controller, controller._session, new InnerDeleteWorker(controller, prompt, files))
            {
            }

            private class InnerDeleteWorker : DeleteWorker
            {
                private readonly BrowserController _controller;
                private readonly List _files;

                public InnerDeleteWorker(BrowserController controller, LoginCallback prompt, List files)
                    : base(controller._session, prompt, files, controller)
                {
                    _controller = controller;
                    _files = files;
                }

                public override void cleanup(object result)
                {
                    if (((Boolean) result).booleanValue())
                    {
                        _controller.RefreshParentPaths((IList<Path>) Utils.ConvertFromJavaList<Path>(_files));
                    }
                }
            }
        }

        private class DisconnectAction : WorkerBackgroundAction
        {
            private readonly BrowserController _controller;

            public DisconnectAction(BrowserController controller, CallbackDelegate callback)
                : base(controller, controller.Session, controller.Cache, new InnerDisconnectWorker(controller, callback)
                    )
            {
                _controller = controller;
            }

            public override void prepare()
            {
                if (null == _controller.Session)
                {
                    throw new ConnectionCanceledException();
                }
                if (!_controller.Session.isConnected())
                {
                    throw new ConnectionCanceledException();
                }
                base.prepare();
            }

            private class InnerDisconnectWorker : DisconnectWorker
            {
                private readonly CallbackDelegate _callback;

                public InnerDisconnectWorker(BrowserController controller, CallbackDelegate callback)
                    : base(controller.Session, controller.Cache)
                {
                    _callback = callback;
                }

                public override void cleanup(object wd)
                {
                    base.cleanup(wd);
                    _callback();
                }
            }
        }

        private class MountAction : WorkerBackgroundAction
        {
            private readonly BrowserController _controller;
            private readonly Host _host;

            public MountAction(BrowserController controller, Session session, Host host)
                : base(controller, controller.Session, new InnerMountWorker(controller, session))
            {
                _controller = controller;
                _host = host;
            }

            public override void init()
            {
                base.init();
                _controller.View.WindowTitle = BookmarkNameProvider.toString(_host, true);
                _controller.View.RefreshBookmark(_controller.Session.getHost());
            }

            private class InnerMountWorker : MountWorker
            {
                private readonly BrowserController _controller;
                private readonly Session _session;

                public InnerMountWorker(BrowserController controller, Session session)
                    : base(session, controller._cache, new DialogLimitedListProgressListener(controller))
                {
                    _controller = controller;
                    _session = session;
                }

                public override void cleanup(object wd)
                {
                    Path workdir = (Path) wd;
                    if (null == workdir)
                    {
                        _controller.Unmount();
                    }
                    else
                    {
                        // Set the working directory
                        _controller.SetWorkdir(workdir);
                        _controller.View.RefreshBookmark(_session.getHost());
                        _controller.ToggleView(BrowserView.File);
                        _controller.View.SecureConnection = _session is SSLSession;
                        _controller.View.CertBasedConnection = _session is SSLSession;
                        _controller.View.SecureConnectionVisible = true;
                    }
                }
            }
        }

        private class MoveAction : WorkerBackgroundAction
        {
            public MoveAction(BrowserController controller, Map selected, IList<Path> changed)
                : base(controller, controller._session, new InnerMoveWorker(controller, selected, changed))
            {
            }

            private class InnerMoveWorker : MoveWorker
            {
                private readonly IList<Path> _changed;
                private readonly BrowserController _controller;
                private readonly Map _files;

                public InnerMoveWorker(BrowserController controller, Map files, IList<Path> changed)
                    : base(controller._session, files, controller)
                {
                    _controller = controller;
                    _files = files;
                    _changed = changed;
                }

                public override void cleanup(object result)
                {
                    _controller.RefreshParentPaths(_changed,
                                                   (IList<Path>) Utils.ConvertFromJavaList<Path>(_files.values()));
                }
            }
        }

        private class ProgressTransferAdapter : TransferAdapter
        {
            private readonly BrowserController _controller;

            public ProgressTransferAdapter(BrowserController controller)
            {
                _controller = controller;
            }

            public override void progress(TransferProgress status)
            {
                _controller.message(status.getProgress());
            }
        }

        private class ReloadTransferCallback : TransferCallback
        {
            private readonly IList<Path> _changed;
            private readonly BrowserController _controller;

            public ReloadTransferCallback(BrowserController controller, IList<Path> changed)
            {
                _controller = controller;
                _changed = changed;
            }


            public void complete(Transfer t)
            {
                _controller.invoke(new ReloadAction(_controller, _changed));
            }

            private class ReloadAction : WindowMainAction
            {
                private readonly IList<Path> _changed;

                public ReloadAction(BrowserController c, IList<Path> changed) : base(c)
                {
                    _changed = changed;
                }

                public override bool isValid()
                {
                    return base.isValid() && ((BrowserController) Controller).IsConnected();
                }

                public override void run()
                {
                    ((BrowserController) Controller).RefreshParentPaths(_changed, _changed);
                }
            }
        }

        private class RevertAction : WorkerBackgroundAction
        {
            public RevertAction(BrowserController controller, IList<Path> files)
                : base(controller, controller._session, new InnerRevertWorker(controller, files))
            {
            }

            private class InnerRevertWorker : RevertWorker
            {
                private readonly BrowserController _controller;
                private readonly IList<Path> _files;

                public InnerRevertWorker(BrowserController controller, IList<Path> files)
                    : base(controller._session, Utils.ConvertToJavaList(files))
                {
                    _controller = controller;
                    _files = files;
                }

                public override void cleanup(object result)
                {
                    _controller.RefreshParentPaths(_files);
                }
            }
        }

        private class UnarchiveAction : BrowserControllerBackgroundAction
        {
            private readonly Archive _archive;
            private readonly List<Path> _expanded;
            private readonly Path _selected;

            public UnarchiveAction(BrowserController controller, Archive archive, Path selected, List<Path> expanded)
                : base(controller)
            {
                _archive = archive;
                _expanded = expanded;
                _selected = selected;
            }

            public override object run()
            {
                ((Compress) BrowserController._session.getFeature(typeof (Compress))).unarchive(_archive, _selected,
                                                                                                BrowserController,
                                                                                                BrowserController);
                return true;
            }

            public override string getActivity()
            {
                return _archive.getDecompressCommand(_selected);
            }

            public override void cleanup()
            {
                base.cleanup();
                _expanded.AddRange(Utils.ConvertFromJavaList<Path>(_archive.getExpanded(new ArrayList {_selected})));
                BrowserController.RefreshParentPaths(_expanded, _expanded);
            }
        }
    }
}
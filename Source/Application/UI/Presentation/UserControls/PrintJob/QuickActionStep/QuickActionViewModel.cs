﻿using pdfforge.Obsidian;
using pdfforge.PDFCreator.Conversion.Jobs.Jobs;
using pdfforge.PDFCreator.Conversion.Settings;
using pdfforge.PDFCreator.Conversion.Settings.Enums;
using pdfforge.PDFCreator.Core.Services;
using pdfforge.PDFCreator.UI.Presentation.Commands;
using pdfforge.PDFCreator.UI.Presentation.Commands.QuickActions;
using pdfforge.PDFCreator.UI.Presentation.Helper;
using pdfforge.PDFCreator.UI.Presentation.Helper.Translation;
using pdfforge.PDFCreator.UI.Presentation.ViewModelBases;
using pdfforge.PDFCreator.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace pdfforge.PDFCreator.UI.Presentation.UserControls.PrintJob.QuickActionStep
{
    public class QuickActionViewModel : TranslatableViewModelBase<QuickActionTranslation>, IWorkflowViewModel
    {
        private readonly ICommandLocator _commandLocator;
        private readonly IReadableFileSizeFormatter _readableFileSizeHelper;
        private readonly ICurrentSettingsProvider _currentSettingsProvider;

        private ICurrentSettings<ObservableCollection<ConversionProfile>> _profilesProvider;
        private OutputFormat _outputFormat = OutputFormat.Pdf;
        private Job _job;

        private string _fileDirectory;
        private string _fileName;
        private string _fileSize;
        private TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>();

        public QuickActionViewModel(ITranslationUpdater translationUpdater, ICommandLocator commandLocator, IReadableFileSizeFormatter readableFileSizeHelper,
            ICurrentSettings<ObservableCollection<ConversionProfile>> profilesProvider, ICurrentSettingsProvider currentSettingsProvider) : base(translationUpdater)
        {
            _commandLocator = commandLocator;
            _readableFileSizeHelper = readableFileSizeHelper;
            _profilesProvider = profilesProvider;
            _currentSettingsProvider = currentSettingsProvider;
            StartQuickActionCommand = new DelegateCommand(StartQuickActionCommandExecute);
            FinishCommand = new DelegateCommand(OnFinish);
            InitList();
        }

        private void OnFinish(object obj)
        {
            _commandLocator.GetCommand<ISaveChangedSettingsCommand>().Execute(null);
            StepFinished?.Invoke(this, EventArgs.Empty);
            _taskCompletionSource.SetResult(null);
        }

        private void InitList()
        {
            QuickActionOpenList = new List<QuickActionListItemVo>
            {
                GetQuickActionItem<QuickActionOpenWithPdfArchitectCommand>(Translation.OpenPDFArchitect),
                GetQuickActionItem<QuickActionOpenWithDefaultCommand>(Translation.OpenDefaultProgram),
                GetQuickActionItem<QuickActionOpenExplorerLocationCommand>(Translation.OpenExplorer)
            };

            QuickActionSendList = new List<QuickActionListItemVo>
            {
                GetQuickActionItem<QuickActionOpenMailClientCommand>(Translation.SendEmail),
                GetQuickActionItem<QuickActionPrintWithPdfArchitectCommand>(Translation.PrintFileWithArchitect)
            };
        }

        private QuickActionListItemVo GetQuickActionItem<TCommand>(string text) where TCommand : class, ICommand
        {
            return new QuickActionListItemVo(text, _commandLocator.GetCommand<TCommand>(), StartQuickActionCommand);
        }

        public void StartQuickActionCommandExecute(object obj)
        {
            var vo = obj as QuickActionListItemVo;
            if (vo != null)
            {
                vo.Command.Execute(_job);
            }
        }

        public Task ExecuteWorkflowStep(Job job)
        {
            _job = job;
            OutputFormat = job.Profile.OutputFormat;
            var firstFile = job.OutputFiles.First();

            FileName = System.IO.Path.GetFileName(firstFile);
            FileDirectory = System.IO.Path.GetDirectoryName(firstFile);
            FileSize = _readableFileSizeHelper.GetFileSizeString(firstFile);
            RaisePropertyChanged(nameof(IsActive));
            return _taskCompletionSource.Task;
        }

        public event EventHandler StepFinished;

        public OutputFormat OutputFormat
        {
            get { return _outputFormat; }
            set
            {
                _outputFormat = value;
                RaisePropertyChanged(nameof(OutputFormat));
            }
        }

        public string FileDirectory
        {
            get { return _fileDirectory; }
            private set
            {
                _fileDirectory = value;
                RaisePropertyChanged(nameof(FileDirectory));
            }
        }

        public string FileSize
        {
            get { return _fileSize; }
            set
            {
                _fileSize = value;
                RaisePropertyChanged(nameof(FileSize));
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                RaisePropertyChanged(nameof(FileName));
            }
        }

        public bool IsActive
        {
            set
            {
                if (_job != null && _currentSettingsProvider != null)
                {
                    var conversionProfile = SettingsHelper.GetProfileByGuid(_profilesProvider.Settings, _job.Profile.Guid);
                    conversionProfile.ShowQuickActions = !value;
                    _job.Profile.ShowQuickActions = !value;
                    RaisePropertyChanged(nameof(IsActive));
                }
            }
            get
            {
                return _job?.Profile?.ShowQuickActions == false;
            }
        }

        public DelegateCommand StartQuickActionCommand { get; }

        public DelegateCommand FinishCommand { get; }

        public IEnumerable<QuickActionListItemVo> QuickActionOpenList { get; private set; }
        public IEnumerable<QuickActionListItemVo> QuickActionSendList { get; private set; }
    }
}

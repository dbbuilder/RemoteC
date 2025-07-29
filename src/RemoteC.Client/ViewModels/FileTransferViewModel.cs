using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using RemoteC.Shared.Models;

namespace RemoteC.Client.ViewModels
{
    public class FileTransferViewModel : ViewModelBase
    {
        private ObservableCollection<FileTransferItemViewModel> _transfers = new();

        public ObservableCollection<FileTransferItemViewModel> Transfers
        {
            get => _transfers;
            set => this.RaiseAndSetIfChanged(ref _transfers, value);
        }
    }

    public class FileTransferItemViewModel : ViewModelBase
    {
        private Guid _transferId;
        private string _fileName = string.Empty;
        private long _fileSize;
        private TransferDirection _direction;
        private TransferStatus _status;
        private int _progress;
        private string _speed = string.Empty;
        private TimeSpan _remainingTime;

        public Guid TransferId
        {
            get => _transferId;
            set => this.RaiseAndSetIfChanged(ref _transferId, value);
        }

        public string FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => this.RaiseAndSetIfChanged(ref _fileSize, value);
        }

        public TransferDirection Direction
        {
            get => _direction;
            set => this.RaiseAndSetIfChanged(ref _direction, value);
        }

        public TransferStatus Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public int Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public string Speed
        {
            get => _speed;
            set => this.RaiseAndSetIfChanged(ref _speed, value);
        }

        public TimeSpan RemainingTime
        {
            get => _remainingTime;
            set => this.RaiseAndSetIfChanged(ref _remainingTime, value);
        }
    }
}
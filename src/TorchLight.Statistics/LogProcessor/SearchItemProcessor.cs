namespace TorchLight.Statistics.LogProcessor;

public class SearchItemProcessor
{
    // public event Action OnSearchStart;
    public event Action<int> OnSerachComplete;

    private bool _inSearchBlock = false;
    private int _itemBaseId = 0;

    public void HandleLine(string line)
    {
        if (line.Contains("----Socket SendMessage STT----XchgSyncSearchPrice----"))
        {
            _inSearchBlock = true;
            return;
        }

        if (_inSearchBlock && line.StartsWith("+itemBaseId ["))
        {
            var match = LineRegex.GetCellValue().Match(line);
            if (match.Success)
            {
                _itemBaseId = int.Parse(match.Groups[1].Value);
                _inSearchBlock = false;
                OnSerachComplete?.Invoke(_itemBaseId);
            }
        }
    }
}
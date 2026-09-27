using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StratagemDeck.Mobile.Models;
using StratagemDeck.Mobile.Services;

namespace StratagemDeck.Mobile.ViewModels;

public class SetupViewModel : INotifyPropertyChanged
{
    private const string AllLetters = "ALL";

    private readonly SessionService _session;
    private readonly PreferencesService _prefs;

    private LoadoutSlot? _selectedSlot;
    private string _status = string.Empty;
    private string _searchQuery = string.Empty;
    private string? _selectedCategory;
    private string _searchLetter = AllLetters;
    private bool _isSearchOpen;
    private bool _useCustomKeyboard = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? SearchCompleted;

    public RangeObservableCollection<Stratagem> AvailableStrats { get; } = new();

    public ICommand SelectSlotCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand TapStratagemCommand { get; }
    public ICommand SaveLoadoutCommand { get; }
    public ICommand ClearLoadoutCommand { get; }
    public ICommand RemoveMissionStratagemCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand OpenSearchCommand { get; }
    public ICommand CloseSearchCommand { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
                RefreshFilter();
            }
        }
    }

    public string SearchLetter
    {
        get => _searchLetter;
        set
        {
            var letter = string.IsNullOrWhiteSpace(value) ? AllLetters : value;
            if (_searchLetter != letter)
            {
                _searchLetter = letter;
                OnPropertyChanged();
                RefreshFilter();
            }
        }
    }

    public bool IsSearchOpen
    {
        get => _isSearchOpen;
        private set
        {
            if (_isSearchOpen == value) return;
            _isSearchOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTabBarVisible));
            OnPropertyChanged(nameof(IsPageContentVisible));
        }
    }

    public bool IsTabBarVisible => !IsSearchOpen;

    public bool IsPageContentVisible => !IsSearchOpen;

    public bool UseCustomKeyboard
    {
        get => _useCustomKeyboard;
        private set
        {
            if (_useCustomKeyboard == value) return;
            _useCustomKeyboard = value;
            OnPropertyChanged();
        }
    }

    public LoadoutSlot? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            _selectedSlot = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSlotIndex));
            OnPropertyChanged(nameof(TargetLabel));
        }
    }

    public int SelectedSlotIndex => SelectedSlot?.SlotIndex ?? -1;
    public bool IsMissionSlotSelected => SelectedSlotIndex == 4;

    public string TargetLabel => SelectedSlot switch
    {
        null => "Pick a slot first",
        { SlotIndex: 4 } => "Mission",
        var slot => $"Slot {slot.SlotIndex + 1}"
    };

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public ObservableCollection<LoadoutSlot> Slots => _session.Slots;
    public ObservableCollection<string> Categories => _session.Categories;

    public ImageSource? Slot0Icon => Slots.Count > 0 ? Slots[0].SlotIcon : null;
    public string Slot0Name => Slots.Count > 0 ? Slots[0].SlotName : "Slot 1";
    public ImageSource? Slot1Icon => Slots.Count > 1 ? Slots[1].SlotIcon : null;
    public string Slot1Name => Slots.Count > 1 ? Slots[1].SlotName : "Slot 2";
    public ImageSource? Slot2Icon => Slots.Count > 2 ? Slots[2].SlotIcon : null;
    public string Slot2Name => Slots.Count > 2 ? Slots[2].SlotName : "Slot 3";
    public ImageSource? Slot3Icon => Slots.Count > 3 ? Slots[3].SlotIcon : null;
    public string Slot3Name => Slots.Count > 3 ? Slots[3].SlotName : "Slot 4";
    public ObservableCollection<Stratagem> MissionStrats => Slots.Count > 4 ? Slots[4].MissionStrats : new ObservableCollection<Stratagem>();

    private void NotifySlotsChanged()
    {
        OnPropertyChanged(nameof(Slot0Icon));
        OnPropertyChanged(nameof(Slot0Name));
        OnPropertyChanged(nameof(Slot1Icon));
        OnPropertyChanged(nameof(Slot1Name));
        OnPropertyChanged(nameof(Slot2Icon));
        OnPropertyChanged(nameof(Slot2Name));
        OnPropertyChanged(nameof(Slot3Icon));
        OnPropertyChanged(nameof(Slot3Name));
        OnPropertyChanged(nameof(MissionStrats));
    }

    public SetupViewModel(SessionService session, PreferencesService prefs)
    {
        _session = session;
        _prefs = prefs;

        SelectSlotCommand = new Command<string>(OnSelectSlot);
        SelectCategoryCommand = new Command<string>(OnSelectCategory);
        TapStratagemCommand = new Command<Stratagem>(OnTapStratagem);
        SaveLoadoutCommand = new Command(OnSaveLoadout);
        ClearLoadoutCommand = new Command(OnClearLoadout);
        RemoveMissionStratagemCommand = new Command<Stratagem>(s => RemoveFromMission(s));
        SearchCommand = new Command(OnSearch);
        OpenSearchCommand = new Command(OnOpenSearch);
        CloseSearchCommand = new Command(OnCloseSearch);

        _session.OnIconsLoaded += NotifySlotsChanged;
    }

    public async Task InitializeAsync()
    {
        UseCustomKeyboard = _prefs.GetUseCustomKeyboard();
        if (IsSearchOpen && !UseCustomKeyboard)
            OnCloseSearch();

        await _session.InitializeAsync();
        await _session.EnsureIconsLoadedAsync();
    }

    private void OnTapStratagem(Stratagem stratagem)
    {
        if (IsMissionSlotSelected)
        {
            AddToMission(stratagem);
        }
        else
        {
            if (SelectedSlot == null)
            {
                var firstEmpty = Slots.FirstOrDefault(
                    s => s.SlotIndex is >= 0 and <= 3 && s.SelectedStratagem == null);

                if (firstEmpty == null)
                {
                    Status = "All slots are full - pick a slot to replace";
                    return;
                }

                SelectedSlot = firstEmpty;
            }

            AssignToSlot(stratagem);
        }

        if (IsSearchOpen)
            OnCloseSearch();
    }

    private void OnOpenSearch()
    {
        if (!UseCustomKeyboard)
            return;

        SearchQuery = string.Empty;
        SearchLetter = AllLetters;
        IsSearchOpen = true;
    }

    private void OnCloseSearch()
    {
        SearchQuery = string.Empty;
        SearchLetter = AllLetters;
        IsSearchOpen = false;
    }

    private void OnSelectSlot(string? indexStr)
    {
        if (!int.TryParse(indexStr, out var idx)) return;
        var slot = Slots.FirstOrDefault(s => s.SlotIndex == idx);
        SelectedSlot = slot;

        if (idx == 4)
            Status = slot?.MissionStrats.Count > 0
                ? "Mission slot selected - tap to add more"
                : "Mission slot selected - tap stratagems to add";
        else
            Status = slot?.SelectedStratagem != null
                ? $"Slot {idx + 1}: {slot.SelectedStratagem.DisplayName}"
                : $"Slot {idx + 1} selected - tap a stratagem";
    }

    private void OnSelectCategory(string? category)
    {
        if (string.IsNullOrEmpty(category)) return;
        _selectedCategory = category;
        RefreshFilter();
    }

    private void OnSearch()
    {
        RefreshFilter();
        SearchCompleted?.Invoke();
    }

    private void RefreshFilter()
    {
        var query = SearchQuery.Trim();
        var hasQuery = query.Length > 0;
        var hasLetter = !string.Equals(SearchLetter, AllLetters, StringComparison.OrdinalIgnoreCase);

        IEnumerable<Stratagem> source;
        if (hasQuery || hasLetter)
        {
            source = _session.GetAll();

            if (hasLetter)
                source = source.Where(s => StartsWithLetter(s, SearchLetter));

            if (hasQuery)
                source = source.Where(s =>
                    s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        else if (_selectedCategory != null)
        {
            source = _session.GetByCategory(_selectedCategory);
        }
        else
        {
            source = Enumerable.Empty<Stratagem>();
        }

        AvailableStrats.ReplaceAll(source);
    }

    private static bool StartsWithLetter(Stratagem stratagem, string letter)
    {
        return stratagem.DisplayName.StartsWith(letter, StringComparison.OrdinalIgnoreCase);
    }

    private void AssignToSlot(Stratagem stratagem)
    {
        if (SelectedSlot == null) return;

        var currentIdx = SelectedSlot.SlotIndex;
        var slot = Slots.FirstOrDefault(s => s.SlotIndex == currentIdx);
        if (slot == null) return;

        slot.SelectedStratagem = stratagem;
        NotifySlotsChanged();
        _session.SaveLoadout();
        Status = $"Assigned {stratagem.DisplayName} to Slot {currentIdx + 1}";

        // Auto-advance to the next empty slot (forward, then wrapping around)
        var next = Slots
            .Where(s => s.SlotIndex is >= 0 and <= 3 && s.SlotIndex > currentIdx && s.SelectedStratagem == null)
            .OrderBy(s => s.SlotIndex)
            .FirstOrDefault()
            ?? Slots
            .Where(s => s.SlotIndex is >= 0 and <= 3 && s.SlotIndex < currentIdx && s.SelectedStratagem == null)
            .OrderBy(s => s.SlotIndex)
            .FirstOrDefault();

        SelectedSlot = next;
        if (next != null)
            Status = $"Slot {next.SlotIndex + 1} selected - tap a stratagem";
    }

    private void AddToMission(Stratagem stratagem)
    {
        var mission = Slots.FirstOrDefault(s => s.SlotIndex == 4);
        if (mission == null) return;

        if (mission.MissionStrats.All(m => m.Name != stratagem.Name || m.Category != stratagem.Category))
        {
            mission.MissionStrats.Add(stratagem);
            NotifySlotsChanged();
            _session.SaveLoadout();
            Status = $"Added {stratagem.DisplayName} to mission";
        }
    }

    private void RemoveFromMission(Stratagem stratagem)
    {
        var mission = Slots.FirstOrDefault(s => s.SlotIndex == 4);
        if (mission == null) return;

        var toRemove = mission.MissionStrats.FirstOrDefault(
            m => m.Name == stratagem.Name && m.Category == stratagem.Category);
        if (toRemove != null)
        {
            mission.MissionStrats.Remove(toRemove);
            NotifySlotsChanged();
            _session.SaveLoadout();
            Status = $"Removed {stratagem.DisplayName} from mission";
        }
    }

    private void OnSaveLoadout()
    {
        _session.SaveLoadout();
        Status = "Loadout saved!";
    }

    private void OnClearLoadout()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            var fresh = new LoadoutSlot
            {
                SlotIndex = Slots[i].SlotIndex,
                Label = Slots[i].Label,
                SelectedStratagem = null,
                MissionStrats = new ObservableCollection<Stratagem>()
            };
            Slots[i] = fresh;
        }

        _session.SaveLoadout();
        NotifySlotsChanged();
        SelectedSlot = null;
        Status = "Loadout cleared";
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

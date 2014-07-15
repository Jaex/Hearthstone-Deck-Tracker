﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Stats;

namespace Hearthstone_Deck_Tracker
{
    /// <summary>
    /// Interaction logic for DeckPicker.xaml
    /// </summary>
    public partial class DeckPicker : ListBox
    {
        public class HsClass
        {
            public List<Deck> Decks;
            public List<string> SelectedTags;
            public Operation TagOperation;
            public string Name;

            public string TagList
            {
                get
                {
                    return "";
                }
            }

            public string GetName
            {
                get
                {
                    return (Name == "Back" || Name == "All")
                               ? Name
                               : Name + " (" +
                                 Decks.Count(
                                     d =>
                                     SelectedTags.Any(t => t == "All") ||
                                     (TagOperation == Operation.Or
                                          ? SelectedTags.Any(t => d.Tags.Contains(t))
                                          : SelectedTags.All(t => d.Tags.Contains(t)))) + ")";
                }
            }

            public string Wins
            {
                get
                {
                    return (Name == "Back" || Name == "All")
                               ? ""
                               : Decks.Sum(
                                   d =>
                                   d.Stats.Iterations.Last()
                                    .GameStats.Count(g => g.GameResult == GameStats.Result.Win)).ToString();
                }
            }

            public string WinsPercentCurrent
            {
                get
                {
                    var gameCount = Decks.Where(DeckMatchesSelectedTags).Sum(
                        d =>
                        d.Stats.Iterations.Last().GameStats.Count);
                    
                    if (gameCount == 0) return "-%";

                    return Math.Round(Decks.Where(DeckMatchesSelectedTags).Sum(
                        d =>
                        d.Stats.Iterations.Last()
                         .GameStats.Count(g => g.GameResult == GameStats.Result.Win)) * 100.0 /
                                      gameCount,
                                      0) + "%";
                }
            }

            public string WinsPercentTotal
            {
                get
                {
                    var gameCount = Decks.Where(DeckMatchesSelectedTags).Sum(
                        d =>
                        d.Stats.Iterations.Sum(i => i.GameStats.Count));
                    
                    if (gameCount == 0) return "(-%)";

                    return "(" + 
                        Math.Round(
                            Decks.Where(DeckMatchesSelectedTags).Sum(
                                d =>
                                d.Stats.Iterations.Sum(
                                    i => i.GameStats.Count(g => g.GameResult == GameStats.Result.Win))) * 100.0 /
                            gameCount, 0) + "%)";
                }
            }

            public string TotalGames
            {
                get
                {
                    return (Name == "Back" || Name == "All")
                               ? ""
                               : Decks.Sum(
                                   d =>
                                   d.Stats.Iterations.Last().GameStats.Count) + " ";
                }
            }

            public string Losses
            {
                get
                {
                    return (Name == "Back" || Name == "All")
                               ? ""
                               : Decks.Sum(
                                   d =>
                                   d.Stats.Iterations.Last().GameStats.Count(g => g.GameResult == GameStats.Result.Loss))
                                      .ToString();
                }
            }

            public Visibility ToolTipVisiblity
            {
                get {return Visibility.Hidden;}
            }
            
            public Color ClassColor
            {
                get
                {
                    switch (Name)
                    {
                        case "Druid":
                            return (Color) ColorConverter.ConvertFromString("#FF7D0A");
                        case "Death Knight":
                            return (Color) ColorConverter.ConvertFromString("#C41F3B");
                        case "Hunter":
                            return (Color) ColorConverter.ConvertFromString("#ABD473");
                        case "Mage":
                            return (Color) ColorConverter.ConvertFromString("#69CCF0");
                        case "Monk":
                            return (Color) ColorConverter.ConvertFromString("#00FF96");
                        case "Paladin":
                            return (Color) ColorConverter.ConvertFromString("#F58CBA");
                        case "Priest":
                            return (Color) ColorConverter.ConvertFromString("#FFFFFF");
                        case "Rogue":
                            return (Color) ColorConverter.ConvertFromString("#FFF569");
                        case "Shaman":
                            return (Color) ColorConverter.ConvertFromString("#0070DE");
                        case "Warlock":
                            return (Color) ColorConverter.ConvertFromString("#9482C9");
                        case "Warrior":
                            return (Color) ColorConverter.ConvertFromString("#C79C6E");
                        default:
                            return Colors.Gray;
                    }
                }
            }
            public FontWeight GetFontWeight
            {
                get { return FontWeights.Bold; }
            }

            public HsClass(string name)
            {
                Name = name;
                Decks = new List<Deck>();
                SelectedTags = new List<string>();
            }

            private bool DeckMatchesSelectedTags(Deck deck)
            {
                return SelectedTags.Any(t => t == "All") ||
                       (TagOperation == Operation.Or
                            ? SelectedTags.Any(t => deck.Tags.Contains(t))
                            : SelectedTags.All(t => deck.Tags.Contains(t)));
            }
        }

        private readonly List<string> _classNames = new List<string> { "Druid", "Hunter", "Mage", "Paladin", "Priest", "Rogue", "Shaman", "Warlock", "Warrior"};
        private readonly List<HsClass> _hsClasses;
        private readonly bool _initialized;
        private bool _inClassSelect;
        public bool ShowAll;
        public List<string> SelectedTags;
        private HsClass _selectedClass;
        public Operation TagOperation;

        public Deck SelectedDeck;

        public delegate void SelectedDeckHandler(DeckPicker sender, Deck deck);

        public event SelectedDeckHandler SelectedDeckChanged;

        public DeckPicker()
        {
            InitializeComponent();

            SelectedTags = new List<string>();

            SelectedDeck = null;
            _hsClasses = new List<HsClass>();
            foreach (var className in _classNames)
            {
                _hsClasses.Add(new HsClass(className));
            }
            _hsClasses.Add(new HsClass("Undefined"));

            var all = new HsClass("All");
            foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
            {
                if (DeckMatchesSelectedTags(d))
                    all.Decks.Add(d);
            } 
            ListboxPicker.Items.Add(all);
            foreach (var hsClass in _hsClasses)
            {
                ListboxPicker.Items.Add(hsClass);
            }
            _inClassSelect = true;
            _initialized = true;
            ShowAll = false;
        }

        public void AddDeck(Deck deck)
        {
            if (deck == null) return;
            var hsClass = _hsClasses.FirstOrDefault(c => c.Name == deck.Class) ?? _hsClasses.First(c => c.Name == "Undefined");
            hsClass.Decks.Add(deck);
        }

        public void AddAndSelectDeck(Deck deck)
        {
            if (deck == null) return;
            AddDeck(deck);
            SelectDeck(deck);
            
        }
        public void SelectDeck(Deck deck)
        {
            if (deck == null) return;
            var hsClass = _hsClasses.FirstOrDefault(c => c.Name == deck.Class) ??
                          _hsClasses.First(c => c.Name == "Undefined");
            
                if (hsClass != null)
                {
                    _selectedClass = hsClass;
                    ListboxPicker.Items.Clear();
                    var back = new HsClass("Back");

                    if (ShowAll)
                    {
                        foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                        {
                            if (DeckMatchesSelectedTags(d))
                                back.Decks.Add(d);
                        }
                        ListboxPicker.Items.Add(back);
                        foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                        {
                            if (DeckMatchesSelectedTags(d))
                                ListboxPicker.Items.Add(d);
                        }
                    }
                    else
                    {
                        foreach (var d in _selectedClass.Decks)
                        {
                            if (DeckMatchesSelectedTags(d))
                                back.Decks.Add(d);
                        }
                        ListboxPicker.Items.Add(back);
                        foreach (var d in _selectedClass.Decks)
                        {
                            if (DeckMatchesSelectedTags(d))
                                ListboxPicker.Items.Add(d);
                        }
                    }
                    ListboxPicker.SelectedItem = deck;
                    _inClassSelect = false;
                }
            
            SelectedDeck = deck;
        }

        private bool DeckMatchesSelectedTags(Deck deck)
        {
            return SelectedTags.Any(t => t == "All") ||
                   (TagOperation == Operation.Or
                        ? SelectedTags.Any(t => deck.Tags.Contains(t))
                        : SelectedTags.All(t => deck.Tags.Contains(t)));
        }

        public void RemoveDeck(Deck deck)
        {
            if (deck == null) return;
            var hsClass = _hsClasses.FirstOrDefault(c => c.Decks.Contains(deck));
            if (hsClass != null)
            {
                hsClass.Decks.Remove(deck);
                
            }
            if (ListboxPicker.Items.Contains(deck))
            {
                ListboxPicker.Items.Remove(deck);
            }
        }
        
        private void ListboxPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListboxPicker.SelectedIndex == -1) return;
            if (!_initialized) return;

            var selectedClass = ListboxPicker.SelectedItem as HsClass;
            if (selectedClass != null)
            {
                if (_inClassSelect)
                {
                    ShowAll = selectedClass.Name == "All";

                    _selectedClass = selectedClass;

                    ListboxPicker.Items.Clear();
                    var back = new HsClass("Back");

                    if (ShowAll)
                    {
                        foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                        {
                            if (DeckMatchesSelectedTags(d))
                                back.Decks.Add(d);
                        }
                        ListboxPicker.Items.Add(back);
                        foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                        {
                            if (DeckMatchesSelectedTags(d))
                                ListboxPicker.Items.Add(d);
                        }
                    }
                    else
                    {
                        foreach (var d in _selectedClass.Decks)
                        {
                            if (DeckMatchesSelectedTags(d))
                                back.Decks.Add(d);
                        }
                        ListboxPicker.Items.Add(back);
                        foreach (var d in _selectedClass.Decks)
                        {
                            if (DeckMatchesSelectedTags(d))
                                ListboxPicker.Items.Add(d);
                        }
                    }
                    _inClassSelect = false;
                }
                else
                {
                    if (selectedClass.Name == "Back")
                    {
                        _selectedClass = null;
                        ListboxPicker.Items.Clear();
                        var all = new HsClass("All");
                        foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                        {
                            if (DeckMatchesSelectedTags(d))
                                all.Decks.Add(d);
                        }
                        ListboxPicker.Items.Add(all);
                        foreach (var hsClass in _hsClasses)
                        {
                            ListboxPicker.Items.Add(hsClass);
                        }
                        _inClassSelect = true;
                    }
                }
            }
            else
            {
                var newSelectedDeck = ListboxPicker.SelectedItem as Deck;
                if (newSelectedDeck != null)
                {
                    if(SelectedDeck != null)
                        SelectedDeck.IsSelectedInGui = false;
                    newSelectedDeck.IsSelectedInGui = true;
                    ListboxPicker.Items.Refresh();

                    if(SelectedDeckChanged != null)
                        SelectedDeckChanged(this, newSelectedDeck);

                    SelectedDeck = newSelectedDeck;
                }
            }
        }
        
        internal void SetSelectedTags(List<string> tags)
        {
            SelectedTags = tags;

            foreach (var hsClass in _hsClasses)
            {
                hsClass.SelectedTags = tags;
            }

            UpdateList();
        }

        public void UpdateList()
        {
            var selectedDeck = SelectedDeck;

            if (!_inClassSelect)
            {
                ListboxPicker.Items.Clear();
                var back = new HsClass("Back");
                if (ShowAll)
                {
                    foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                    {
                        if (DeckMatchesSelectedTags(d))
                            back.Decks.Add(d);
                    }
                    ListboxPicker.Items.Add(back);
                    foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                    {
                        if (DeckMatchesSelectedTags(d))
                            ListboxPicker.Items.Add(d);
                    }
                }
                else
                {
                    foreach (var d in _selectedClass.Decks)
                    {
                        if (DeckMatchesSelectedTags(d))
                            back.Decks.Add(d);
                    }
                    ListboxPicker.Items.Add(back);
                    foreach (var d in _selectedClass.Decks)
                    {
                        if (DeckMatchesSelectedTags(d))
                            ListboxPicker.Items.Add(d);
                    }
                }
            }
            else
            {
                _selectedClass = null;
                ListboxPicker.Items.Clear(); 
                var all = new HsClass("All");
                foreach (var d in _hsClasses.SelectMany(hsc => hsc.Decks))
                {
                    if (DeckMatchesSelectedTags(d))
                        all.Decks.Add(d);
                }
                ListboxPicker.Items.Add(all);
                foreach (var hsClass in _hsClasses)
                {
                    ListboxPicker.Items.Add(hsClass);
                }
                _inClassSelect = true;
            }

            if(selectedDeck != null)
                SelectDeck(selectedDeck);
        }

        public void SetTagOperation(Operation o)
        {
            TagOperation = o;
            foreach (var hsClass in _hsClasses)
            {
                hsClass.TagOperation = o;
            }
        }
    }
}

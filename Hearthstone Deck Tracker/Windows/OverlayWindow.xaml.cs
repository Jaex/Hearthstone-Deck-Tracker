﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using Hearthstone_Deck_Tracker.Hearthstone;

#endregion

namespace Hearthstone_Deck_Tracker
{
    /// <summary>
    ///     Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow
    {
        private readonly List<HearthstoneTextBlock> _cardLabels;
        private readonly List<HearthstoneTextBlock> _cardMarkLabels;
        private readonly List<StackPanel> _stackPanelsMarks;
        private readonly Config _config;
        private readonly int _customHeight;
        private readonly int _customWidth;
        private readonly Game _game;
        private readonly int _offsetX;
        private readonly int _offsetY;
        private int _cardCount;
        private int _opponentCardCount;

        public OverlayWindow(Config config, Game game)
        {
            InitializeComponent();
            _config = config;
            _game = game;

            ListViewPlayer.ItemsSource = _game.IsUsingPremade
                                             ? _game.PlayerDeck
                                             : _game.PlayerDrawn;
            ListViewOpponent.ItemsSource = _game.EnemyCards;
            Scaling = 1.0;
            OpponentScaling = 1.0;
            ShowInTaskbar = _config.ShowInTaskbar;
            if (_config.VisibleOverlay)
            {
                Background = (SolidColorBrush) new BrushConverter().ConvertFrom("#4C0000FF");
            }
            _offsetX = _config.OffsetX;
            _offsetY = _config.OffsetY;
            _customWidth = _config.CustomWidth;
            _customHeight = _config.CustomHeight;

            _cardLabels = new List<HearthstoneTextBlock>
                {
                    LblCard0,
                    LblCard1,
                    LblCard2,
                    LblCard3,
                    LblCard4,
                    LblCard5,
                    LblCard6,
                    LblCard7,
                    LblCard8,
                    LblCard9,
                };
            _cardMarkLabels = new List<HearthstoneTextBlock>
                {
                    LblCardMark0,
                    LblCardMark1,
                    LblCardMark2,
                    LblCardMark3,
                    LblCardMark4,
                    LblCardMark5,
                    LblCardMark6,
                    LblCardMark7,
                    LblCardMark8,
                    LblCardMark9,
                };
            _stackPanelsMarks = new List<StackPanel>
                {
                    Marks0,
                    Marks1,
                    Marks2,
                    Marks3,
                    Marks4,
                    Marks5,
                    Marks6,
                    Marks7,
                    Marks8,
                    Marks9,
                };

            UpdateScaling();
        }

        public static double Scaling { get; set; }
        public static double OpponentScaling { get; set; }

        public void SortViews()
        {
            SortCardCollection(ListViewPlayer.ItemsSource);
            SortCardCollection(ListViewOpponent.ItemsSource);
        }

        private void SortCardCollection(IEnumerable collection)
        {
            var view1 = (CollectionView) CollectionViewSource.GetDefaultView(collection);
            view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view1.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Descending));
            view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void SetEnemyCardCount(int cardCount, int cardsLeftInDeck)
        {
            //previous cardcout > current -> enemy played -> resort list
            if (_opponentCardCount > cardCount)
            {
                SortCardCollection(ListViewOpponent.ItemsSource);
            }
            _opponentCardCount = cardCount;

            LblOpponentCardCount.Text = "Hand: " + cardCount;
            LblOpponentDeckCount.Text = "Deck: " + cardsLeftInDeck;


            if (cardsLeftInDeck <= 0) return;

            var handWithoutCoin = cardCount - (_game.OpponentHasCoin ? 1 : 0);
            

            var holdingNextTurn2 =
                Math.Round(100.0f*Helper.DrawProbability(2, (cardsLeftInDeck + handWithoutCoin), handWithoutCoin + 1), 2);
            var drawNextTurn2 = Math.Round(200.0f/cardsLeftInDeck, 2);
            LblOpponentDrawChance2.Text = "[2]: " + holdingNextTurn2 + "% / " + drawNextTurn2 + "%";

            var holdingNextTurn =
                Math.Round(100.0f*Helper.DrawProbability(1, (cardsLeftInDeck + handWithoutCoin), handWithoutCoin + 1), 2);
            var drawNextTurn = Math.Round(100.0f/cardsLeftInDeck, 2);
            LblOpponentDrawChance1.Text = "[1]: " + holdingNextTurn + "% / " + drawNextTurn + "%";
        }


        private void SetCardCount(int cardCount, int cardsLeftInDeck)
        {
            //previous < current -> draw
            if (_cardCount < cardCount)
            {
                SortCardCollection(ListViewPlayer.ItemsSource);
            }
            _cardCount = cardCount;
            LblCardCount.Text = "Hand: " + cardCount;
            LblDeckCount.Text = "Deck: " + cardsLeftInDeck;

            if (cardsLeftInDeck <= 0) return;

            LblDrawChance2.Text = "[2]: " + Math.Round(200.0f/cardsLeftInDeck, 2) + "%";
            LblDrawChance1.Text = "[1]: " + Math.Round(100.0f/cardsLeftInDeck, 2) + "%";
        }


        public void ShowOverlay(bool enable)
        {
            if (enable)
                Show();
            else Hide();
        }

        private void SetRect(int top, int left, int width, int height)
        {
            Top = top + _offsetY;
            Left = left + _offsetX;
            Width = (_customWidth == -1) ? width : _customWidth;
            Height = (_customHeight == -1) ? height : _customHeight;
            CanvasInfo.Height = (_customHeight == -1) ? height : _customHeight;
            CanvasInfo.Width = (_customWidth == -1) ? width : _customWidth;
        }

        private void ReSizePosLists()
        {
            //player
            if (((Height*_config.PlayerDeckHeight/(_config.OverlayPlayerScaling/100)/100) -
                 (ListViewPlayer.Items.Count*35*Scaling)) < 1 || Scaling < 1)
            {
                var previousScaling = Scaling;
                Scaling = (Height*_config.PlayerDeckHeight/(_config.OverlayPlayerScaling/100)/100)/
                          (ListViewPlayer.Items.Count*35);
                if (Scaling > 1)
                    Scaling = 1;

                if (previousScaling != Scaling)
                    ListViewPlayer.Items.Refresh();
            }

            Canvas.SetTop(StackPanelPlayer, Height*_config.PlayerDeckTop/100);
            Canvas.SetLeft(StackPanelPlayer,
                           Width*_config.PlayerDeckLeft/100 -
                           StackPanelPlayer.ActualWidth*_config.OverlayPlayerScaling/100);

            //opponent
            if (((Height*_config.OpponentDeckHeight/(_config.OverlayOpponentScaling/100)/100) -
                 (ListViewOpponent.Items.Count*35*OpponentScaling)) < 1 || OpponentScaling < 1)
            {
                var previousScaling = OpponentScaling;
                OpponentScaling = (Height*_config.OpponentDeckHeight/(_config.OverlayOpponentScaling/100)/100)/
                                  (ListViewOpponent.Items.Count*35);
                if (OpponentScaling > 1)
                    OpponentScaling = 1;

                if (previousScaling != OpponentScaling)
                    ListViewOpponent.Items.Refresh();
            }


            Canvas.SetTop(StackPanelOpponent, Height*_config.OpponentDeckTop/100);
            Canvas.SetLeft(StackPanelOpponent, Width*_config.OpponentDeckLeft/100);

            // Timers
            Canvas.SetTop(LblTurnTime,
                          (Height - SystemParameters.WindowCaptionHeight)*_config.TimersVerticalPosition/100 - 5);
            Canvas.SetLeft(LblTurnTime, Width*_config.TimersHorizontalPosition/100);

            Canvas.SetTop(LblOpponentTurnTime,
                          (Height - SystemParameters.WindowCaptionHeight)*_config.TimersVerticalPosition/100 -
                          _config.TimersVerticalSpacing);
            Canvas.SetLeft(LblOpponentTurnTime,
                           (Width*_config.TimersHorizontalPosition/100) + _config.TimersHorizontalSpacing);

            Canvas.SetTop(LblPlayerTurnTime,
                          (Height - SystemParameters.WindowCaptionHeight)*_config.TimersVerticalPosition/100 +
                          _config.TimersVerticalSpacing);
            Canvas.SetLeft(LblPlayerTurnTime,
                           Width*_config.TimersHorizontalPosition/100 + _config.TimersHorizontalSpacing);


            Canvas.SetTop(LblGrid,
                          (Helper.IsFullscreen("Hearthstone")
                               ? Height*0.03
                               : Height*0.03 + SystemParameters.CaptionHeight));


            var ratio = Width/Height;
            LblGrid.Width = ratio < 1.5 ? Width*0.3 : Width*0.15*(ratio/1.33);
        }

        private void Window_SourceInitialized_1(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            User32.SetWindowExTransparent(hwnd);
        }

        public void Update(bool refresh)
        {
            if (refresh)
            {
                ListViewPlayer.Items.Refresh();
                ListViewOpponent.Items.Refresh();
                Topmost = false;
                Topmost = true;
                Logger.WriteLine("Refreshed overlay topmost status");
            }


            var handCount = _game.EnemyHandCount;
            if (handCount < 0) handCount = 0;
            if (handCount > 10) handCount = 10;
            //offset label-grid based on handcount
            Canvas.SetLeft(LblGrid, Width/2 - LblGrid.ActualWidth/2 - Width*0.002*handCount);

            var labelDistance = LblGrid.Width/(handCount + 1);

            for (int i = 0; i < handCount; i++)
            {
                var offset = labelDistance*Math.Abs(i - handCount/2);
                offset = offset*offset*0.0015;


                if (handCount%2 == 0)
                {
                    if (i < handCount/2 - 1)
                    {
                        //even hand count -> both middle labels at same height
                        offset = labelDistance*Math.Abs(i - (handCount/2 - 1));
                        offset = offset*offset*0.0015;
                        _stackPanelsMarks[i].Margin = new Thickness(0, -offset, 0, 0);
                    }
                    else if (i > handCount/2)
                    {
                        _stackPanelsMarks[i].Margin = new Thickness(0, -offset, 0, 0);
                    }
                    else
                    {
                        var left = (handCount == 2 && i == 0) ? Width*0.02 : 0;
                        _stackPanelsMarks[i].Margin = new Thickness(left, 0, 0, 0);
                    }
                }
                else
                {
                    _stackPanelsMarks[i].Margin = new Thickness(0, -offset, 0, 0);
                }

                if (!_config.HideOpponentCardAge)
                {
                    _cardLabels[i].Text = _game.OpponentHandAge[i].ToString();
                    _cardLabels[i].Visibility = Visibility.Visible;
                }
                else
                {
                    _cardLabels[i].Visibility = Visibility.Collapsed;
                }

                if (!_config.HideOpponentCardMarks)
                {
                    _cardMarkLabels[i].Text = ((char)_game.OpponentHandMarks[i]).ToString();
                    _cardMarkLabels[i].Visibility = Visibility.Visible;
                }
                else
                {
                    _cardMarkLabels[i].Visibility = Visibility.Collapsed;
                }

                _stackPanelsMarks[i].Visibility = Visibility.Visible;
            }
            for (int i = handCount; i < 10; i++)
            {
                _stackPanelsMarks[i].Visibility = Visibility.Collapsed;
            }

            StackPanelPlayer.Opacity = _config.PlayerOpacity/100;
            StackPanelOpponent.Opacity = _config.OpponentOpacity/100;
            Opacity = _config.OverlayOpacity/100;

            StackPanelPlayer.Visibility = _config.HideDecksInOverlay ? Visibility.Collapsed : Visibility.Visible;
            StackPanelOpponent.Visibility = _config.HideDecksInOverlay ? Visibility.Collapsed : Visibility.Visible;

            LblDrawChance1.Visibility = _config.HideDrawChances ? Visibility.Collapsed : Visibility.Visible;
            LblDrawChance2.Visibility = _config.HideDrawChances ? Visibility.Collapsed : Visibility.Visible;
            LblCardCount.Visibility = _config.HidePlayerCardCount ? Visibility.Collapsed : Visibility.Visible;
            LblDeckCount.Visibility = _config.HidePlayerCardCount ? Visibility.Collapsed : Visibility.Visible;

            LblOpponentDrawChance1.Visibility = _config.HideOpponentDrawChances
                                                    ? Visibility.Collapsed
                                                    : Visibility.Visible;
            LblOpponentDrawChance2.Visibility = _config.HideOpponentDrawChances
                                                    ? Visibility.Collapsed
                                                    : Visibility.Visible;
            LblOpponentCardCount.Visibility = _config.HideEnemyCardCount ? Visibility.Collapsed : Visibility.Visible;
            LblOpponentDeckCount.Visibility = _config.HideEnemyCardCount ? Visibility.Collapsed : Visibility.Visible;

            ListViewOpponent.Visibility = _config.HideEnemyCards ? Visibility.Collapsed : Visibility.Visible;
            ListViewPlayer.Visibility = _config.HidePlayerCards ? Visibility.Collapsed : Visibility.Visible;

            LblGrid.Visibility = _game.IsInMenu ? Visibility.Hidden : Visibility.Visible;

            DebugViewer.Visibility = _config.Debug ? Visibility.Visible : Visibility.Hidden;
            DebugViewer.Width = (Width*_config.TimerLeft/100);

            SetCardCount(_game.PlayerHandCount,
                         _game.IsUsingPremade
                             ? _game.PlayerDeck.Sum(c => c.Count)
                             : 30 - _game.PlayerDrawn.Sum(c => c.Count));

            SetEnemyCardCount(_game.EnemyHandCount, _game.OpponentDeckCount);

            ReSizePosLists();
        }


        public void UpdatePosition()
        {
            //hide the overlay depenting on options
            ShowOverlay(!(
                             (_config.HideInBackground && !User32.IsForegroundWindow("Hearthstone"))
                             || (_config.HideInMenu && _game.IsInMenu)
                             || _config.HideOverlay));

            var hsRect = new User32.Rect();
            User32.GetWindowRect(User32.FindWindow(null, "Hearthstone"), ref hsRect);

            //hs window has height 0 if it just launched, screwing things up if the tracker is started before hs is. 
            //this prevents that from happening. 
            if (hsRect.bottom - hsRect.top == 0)
            {
                return;
            }

            SetRect(hsRect.top, hsRect.left, hsRect.right - hsRect.left, hsRect.bottom - hsRect.top);
            ReSizePosLists();
        }


        internal void UpdateTurnTimer(TimerEventArgs timerEventArgs)
        {
            if (timerEventArgs.Running && (timerEventArgs.PlayerSeconds > 0 || timerEventArgs.OpponentSeconds > 0))
            {
                ShowTimers();

                LblTurnTime.Text = string.Format("{0:00}:{1:00}", (timerEventArgs.Seconds/60)%60,
                                                 timerEventArgs.Seconds%60);
                LblPlayerTurnTime.Text = string.Format("{0:00}:{1:00}", (timerEventArgs.PlayerSeconds/60)%60,
                                                       timerEventArgs.PlayerSeconds%60);
                LblOpponentTurnTime.Text = string.Format("{0:00}:{1:00}", (timerEventArgs.OpponentSeconds/60)%60,
                                                         timerEventArgs.OpponentSeconds%60);

                if (_config.Debug)
                {
                    LblDebugLog.Text += string.Format("Current turn: {0} {1} {2} \n",
                                                      timerEventArgs.CurrentTurn.ToString(),
                                                      timerEventArgs.PlayerSeconds.ToString(),
                                                      timerEventArgs.OpponentSeconds.ToString());
                    DebugViewer.ScrollToBottom();
                }
            }
        }

        public void UpdateScaling()
        {
            StackPanelPlayer.RenderTransform = new ScaleTransform(_config.OverlayPlayerScaling/100,
                                                                  _config.OverlayPlayerScaling/100);
            StackPanelOpponent.RenderTransform = new ScaleTransform(_config.OverlayOpponentScaling/100,
                                                                    _config.OverlayOpponentScaling/100);
        }

        public void HideTimers()
        {
            LblPlayerTurnTime.Visibility = LblOpponentTurnTime.Visibility = LblTurnTime.Visibility = Visibility.Hidden;
        }

        public void ShowTimers()
        {
            LblPlayerTurnTime.Visibility =
                LblOpponentTurnTime.Visibility =
                LblTurnTime.Visibility = _config.HideTimers ? Visibility.Hidden : Visibility.Visible;
        }

        public void SetOpponentTextLocation(bool top)
        {
            StackPanelOpponent.Children.Clear();
            if (top)
            {
                StackPanelOpponent.Children.Add(LblOpponentDrawChance2);
                StackPanelOpponent.Children.Add(LblOpponentDrawChance1);
                StackPanelOpponent.Children.Add(StackPanelOpponentCount);
                StackPanelOpponent.Children.Add(ListViewOpponent);
            }
            else
            {
                StackPanelOpponent.Children.Add(ListViewOpponent);
                StackPanelOpponent.Children.Add(LblOpponentDrawChance2);
                StackPanelOpponent.Children.Add(LblOpponentDrawChance1);
                StackPanelOpponent.Children.Add(StackPanelOpponentCount);
            }
        }

        public void SetPlayerTextLocation(bool top)
        {
            StackPanelPlayer.Children.Clear();
            if (top)
            {
                StackPanelPlayer.Children.Add(StackPanelPlayerDraw);
                StackPanelPlayer.Children.Add(StackPanelPlayerCount);
                StackPanelPlayer.Children.Add(ListViewPlayer);
            }
            else
            {
                StackPanelPlayer.Children.Add(ListViewPlayer);
                StackPanelPlayer.Children.Add(StackPanelPlayerDraw);
                StackPanelPlayer.Children.Add(StackPanelPlayerCount);
            }
        }
    }
}
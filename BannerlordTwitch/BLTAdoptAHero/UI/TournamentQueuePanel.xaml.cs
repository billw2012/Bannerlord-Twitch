using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace BLTAdoptAHero.UI
{
    public partial class TournamentQueuePanel
    {
        public TournamentQueuePanel()
        {
            InitializeComponent();
        }

        private class TournamentSlot
        {
            public Brush Color => !Occupied
                ? Brushes.Gray
                : Position == 15
                    ? Brushes.Coral
                    : Position > 15
                        ? Brushes.Teal
                        : Brushes.White;
            public int Position { get; set; }
            public bool Occupied { get; set; }
        }
        
        public void UpdateTournamentQueue(int queueLength)
        {
            var items = new List<TournamentSlot>();
            for (int i = 0; i < Math.Max(16, queueLength); i++)
            {
                items.Add(new TournamentSlot
                {
                    Occupied = queueLength > i,
                    Position = i,
                });
            }
            TournamentList.ItemsSource = items;
        }
    }
}
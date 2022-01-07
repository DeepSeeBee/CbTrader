using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CbTrader.Utils;

namespace CbTrader.Weight
{
    internal sealed class CWeightTableRow : CViewModel
    {
        #region ctor
        internal CWeightTableRow(CWeightTableVm aWeightTableVm)
        {
            this.ParentWeightTableVm = aWeightTableVm;
        }
        #endregion
        #region Environ
        internal readonly CWeightTableVm ParentWeightTableVm;
        #endregion
        #region Weight
        private double WeightM;
        internal double WeightInternal
        {
            get => this.WeightM;
            set
            {
                this.WeightM = value;
                this.OnPropertyChanged(nameof(this.VmWeight));
            }
        }
        internal double Weight
        {
            get => this.WeightM;
            set
            {
                this.WeightInternal = value;
                this.ParentWeightTableVm.UpdateInterpolatedWeights();
                this.ParentWeightTableVm.ParentTrader.LimitOrdersVms.RefreshWeights();
            }
        }
        public double VmWeight
        {
            get => this.Weight;
            set => this.Weight = value;
        }
        #endregion
        #region InterpolatedWeight
        private double? InterpolatedWeightM;
        internal double InterpolatedWeight
        {
            get => this.InterpolatedWeightM.Value;
            set
            {
                this.InterpolatedWeightM = value;
                this.WeightInternal = value;
                this.OnPropertyChanged(nameof(this.VmInterpolateableWeight));
            }
        }
        #endregion
        #region InterpolateableWeight
        internal double InterpolateableWeight
            => this.Active ? this.Weight : this.InterpolatedWeight;
        public double VmInterpolateableWeight
        {
            get => this.InterpolateableWeight;
            set
            {
                if (this.Active)
                {
                    this.Weight = value;
                }
            }
        }
        internal void RefreshInterpolateableWeight()
        {
            this.OnPropertyChanged(nameof(this.VmInterpolateableWeight));
        }
        #endregion
        #region OrderIdxFkt
        internal double IndexFkt => (double)this.Index / (double)(this.ParentWeightTableVm.Count - 1);
        #endregion
        #region Index
        private int Index => this.ParentWeightTableVm.Items.IndexOfNullable(this).Value;
        internal bool IsFirst => this.Index == 0;
        internal bool IsLast => this.Index == this.ParentWeightTableVm.Items.Length - 1;
        internal bool IsLocked => this.IsFirst || this.IsLast;   
        #endregion
        #region Active
        private bool? ActiveM;
        internal bool Active
        {
            get => CLazyLoad.Get(ref this.ActiveM, () => this.IsLocked ? true : false);
            set
            {
                if (this.IsLocked)
                    throw new InvalidOperationException();
                this.ActiveM = value;
                this.OnPropertyChanged(nameof(this.VmActive));
                this.ParentWeightTableVm.UpdateInterpolatedWeights();
                this.RefreshInterpolateableWeight();
            }
        }
        public bool VmActive
        {
            get => this.Active;
            set => this.Active = value;
        }
        public bool VmActiveIsEditable => !this.IsLocked;
        #endregion
        #region Interpolate
        internal void Interpolate(CWeightTableRow aItemLo, CWeightTableRow aItemHi)
            => this.InterpolatedWeight = this.IndexFkt.Interpolate(aItemLo.IndexFkt, aItemHi.IndexFkt, aItemLo.InterpolateableWeight, aItemHi.InterpolateableWeight);
        #endregion
    }

    internal sealed class CWeightTableVm: CViewModel
    {
        #region ctor
        internal CWeightTableVm(CWeightTablesVm aWeightTablesVm)
        {
            this.ParentWeightTablesVm = aWeightTablesVm;
            this.Init();
        }
        internal CWeightTableVm(CWeightTablesVm aWeightTablesVm, params CWeightTableRow[] aRows): this(aWeightTablesVm)
        {
            this.ItemsM = aRows;
            this.Init();
        }
        private void Init()
        {
            this.UpdateInterpolatedWeights();
        }
        #endregion
        #region Environ
        internal readonly CWeightTablesVm ParentWeightTablesVm;
        internal CCbTrader ParentTrader => this.ParentWeightTablesVm.ParentTrader;
        internal int RowCount => this.ParentWeightTablesVm.RowCount;
        #endregion
        #region Items
        private CWeightTableRow[] ItemsM;
        internal CWeightTableRow[] Items => CLazyLoad.Get(ref this.ItemsM, this.NewItems);
        private CWeightTableRow[] NewItems()
        {
            var aRows = Enumerable.Range(0, this.RowCount).Select(i => new CWeightTableRow(this)).ToArray();

            return aRows;
        }
        public IEnumerable<CWeightTableRow> VmItems => this.Items;
        #endregion
        internal int Count => this.Items.Length;

        #region Interpolate
        internal CWeightTableVm Interpolate(int aCount)
        {
            if (this.Count == aCount)
                return new CWeightTableVm(this.ParentWeightTablesVm, (CWeightTableRow[])this.Items.Clone());
            else
                throw new NotImplementedException();
        }

        internal static CWeightTableVm Interpolate(CWeightTableVm lhs, CWeightTableVm rhs)
        {
            throw new NotImplementedException();
        }

        internal void UpdateInterpolatedWeights()
        {
            var aItems = this.Items;
            var aItemLo = default(CWeightTableRow);
            foreach(var aItemIdx in Enumerable.Range(0,aItems.Length))
            {
                var aItem = aItems[aItemIdx];
                if(aItem.Active)
                {
                    aItemLo = aItem;
                }
                else
                {
                    var aItemHi = aItems.Skip(aItemIdx + 1).Where(i => i.Active).First();
                    aItem.Interpolate(aItemLo, aItemHi);
                }
            }
        }

        internal void RefreshInterpolateableWeights()
        {
            foreach(var aItem in this.Items)
            {
                aItem.RefreshInterpolateableWeight();
            }
        }

        #endregion
        #region Pitch
        private double PitchM;
        internal double Pitch
        {
            get => this.PitchM;
            set
            {
                var aItemNullable = this.ParentWeightTablesVm.ItemNullable;
                this.PitchM = value;
                this.OnPropertyChanged(nameof(this.VmPitch));
                this.OnPropertyChanged(nameof(this.VmPitchText));
                this.ParentWeightTablesVm.RefreshOrderedItems();
                this.ParentWeightTablesVm.ItemNullable = aItemNullable;
            }
        }
        public double VmPitch
        {
            get => this.Pitch;
            set => this.Pitch = value;
        }
        public string VmPitchText
        {
            get => this.Pitch.ToString(this.ParentTrader.SettingsVm.DigitFormat);
            set => this.Pitch = double.Parse(value);
        }
        public bool VmPitchIsEditable => !this.IsFirst && !this.IsLast;
        #endregion
        #region Index
        private int Index => this.ParentWeightTablesVm.Items.IndexOfNullable(this).Value;
        internal bool IsFirst => this.Index == 0;
        internal bool IsLast => this.Index == this.ParentWeightTablesVm.Items.Count - 1;
        internal bool IsLocked => this.IsFirst || this.IsLast;
        #endregion
    }

    internal sealed class CWeightTablesVm : CViewModel
    {
        #region ctor
        internal CWeightTablesVm(CCbTrader aTrader)
        {
            this.ParentTrader = aTrader;
        }
        #endregion
        internal readonly CCbTrader ParentTrader;
        internal int RowCount => this.ParentTrader.SettingsVm.TradeCount;
        #region Items
        internal readonly ObservableCollection<CWeightTableVm> Items = new ObservableCollection<CWeightTableVm>();
        internal IEnumerable<CWeightTableVm> OrderedItems => this.Items.OrderBy(i => i.Pitch);
        public IEnumerable<CWeightTableVm> VmItems => this.OrderedItems;
        internal void RefreshOrderedItems()
        {
            this.OnPropertyChanged(nameof(this.VmItems));
        }
        #endregion
        #region Item
        internal CWeightTableVm ItemNullable
        {
            get => this.ItemIndex.HasValue  ? this.OrderedItems.ElementAt(this.ItemIndex.Value) : default;
            set => this.ItemIndex = this.OrderedItems.IndexOfNullable(value);
        }

        public object VmItem => this.ItemNullable;
        private void OnItemChanged()
        {
            this.OnPropertyChanged(nameof(this.VmItem));
        }
        #endregion
        #region ItemIndex
        private int? ItemIndexM;
        internal int? ItemIndex
        {
            get => this.CoerceItemIndex(this.ItemIndexM);
            set
            {
                this.ItemIndexM = this.CoerceItemIndex(value);
                this.OnPropertyChanged(nameof(this.VmItemIndex1b));
                this.OnPropertyChanged(nameof(this.VmItem));
                this.OnPropertyChanged(nameof(this.VmRemoveTableIsEnabled));
            }
        }
        private int? ItemIndexDefault => this.Items.Count > 0 ? new int?(1) : default(int?);
        private int? CoerceItemIndex(int? i)
        {
            if(!i.HasValue)
            {
                return this.ItemIndexDefault;   
            }
            else if(i.Value < 0 || i.Value >= this.ItemCount)
            {
                return default;
            }
            else
            {
                return i;
            }
        }
        public int? VmItemIndex1b
        {
            get => this.ItemIndex.HasValue  ? this.ItemIndex.Value + 1:  default(int?);
            set => this.ItemIndex = value - 1;
        }
        #endregion
        #region ItemCount
        internal int ItemCount
        {
            get => this.Items.Count();
        }
        public int VmItemCount
        {
            get => this.ItemCount;
        }
        private void OnItemCountChanged()
        {
            this.OnPropertyChanged(nameof(this.VmItemCount));
        }
        #endregion

        private double PitchDefault
        {
            get
            {
                var aHi = this.Items.Last();
                var aLo = this.Items.Reverse().Skip(1).First();
                var p1 = aLo.Pitch;
                var p2 = aHi.Pitch;
                var p = p1+(p2 - p1) / 2d;
                return p;
            }
        }
        internal CWeightTableVm Add()
            =>this.Add(this.ItemCount - 1, this.PitchDefault);
        private CWeightTableVm Add(int aIndex, double aPitch)
        {
            var aWeightTableVm = new CWeightTableVm(this);
            aWeightTableVm.Pitch = aPitch;
            this.Items.Insert(aIndex, aWeightTableVm);

            this.OnItemCountChanged();
            this.ItemIndex = aIndex;
            return aWeightTableVm;
        }

        public bool VmRemoveTableIsEnabled => this.ItemNullable is object && !this.ItemNullable.IsLocked;
        internal void Remove()
        {
            var aItem = this.ItemNullable;
            var aIsSelected = object.ReferenceEquals(aItem, this.ItemNullable);
            this.Items.Remove(aItem);
            if(aIsSelected)
            {
                this.ItemIndex = this.ItemIndexDefault;
            }
            this.OnItemCountChanged();
        }

        internal void LoadDefaults(bool aInit = false)
        {
            this.Items.Clear();
            {
                var aLo = this.Add(0, -1d);
                var aLastRow = aLo.Items.Last();
                aLastRow.WeightInternal = 1.0d;
            }
            {
                var aMid = this.Add(1, 0.0d);
                var aMidRow = aMid.Items.ElementAt(aMid.Items.Count() / 2);
                aMidRow.Active = true;
                aMidRow.WeightInternal = 1;
            }
            {
                var aHi = this.Add(2, 1.0d);
                var aFirstRow = aHi.Items.First();
                aFirstRow.WeightInternal = 1.0d;
            }

            this.ItemIndex = 0;

            this.UpdateInterpolatedWeights();

            if (!aInit)
            {
                this.ParentTrader.LimitOrdersVms.RefreshWeights();
            }
        }

        internal void UpdateInterpolatedWeights()
        {
            foreach(var aTable in this.Items)
            {
                aTable.UpdateInterpolatedWeights();
            }
        }

        internal double[] GetWeights(double aPitch)
        {
            var aHi = this.Items.Where(i => i.Pitch >= aPitch).First();
            var aHiIdx = this.Items.IndexOfNullable(aHi).Value;
            var aLoIdx = aHiIdx - 1;
            var aLo = aLoIdx < 0 ? aHi : this.Items.ElementAt(aLoIdx);
            var aHiPitch = aHi.Pitch;
            var aLoPitch = aLo.Pitch;
            var aDeltaPitch = aHiPitch - aLoPitch;
            var aFaktor = aDeltaPitch== 0 ? 1: (aPitch - aLoPitch) / aDeltaPitch;
            var aWeightRanges = Enumerable.Range(0, this.RowCount)
                .Select(i => new Tuple<double, double>(aLo.Items[i].Weight, aHi.Items[i].Weight));
            var aWeights = aWeightRanges.Select(r => r.Item1 + (r.Item2 - r.Item1) * aFaktor).ToArray();
            return aWeights;
        }

    }

}

namespace CbTrader
{
    using CbTrader.Weight;

    partial class CCbTrader
    {
        #region WeightTablesVm
        private CWeightTablesVm WeightTablesVmM;
        internal CWeightTablesVm WeightTablesVm
        {
            get => CLazyLoad.Get(ref this.WeightTablesVmM, this.NewWeightTables);
            set
            {
                this.WeightTablesVmM = value;
                this.OnPropertyChanged(nameof(this.VmWeightTablesVm));
            }
        }
        public object VmWeightTablesVm => this.WeightTablesVm;
        private CWeightTablesVm NewWeightTables()
        {
            var aWeightTables = new CWeightTablesVm(this);
            aWeightTables.LoadDefaults(true);
            return aWeightTables;
        }
        #endregion
        #region WeightTablesEditIsActive
        private bool WeightTablesEditIsActiveM;
        public bool WeightTablesEditIsActive
        {
            get => this.WeightTablesEditIsActiveM;
            set
            {
                this.WeightTablesEditIsActiveM = value;
                this.OnPropertyChanged(nameof(this.VmWeightTablesEditIsActive));
                this.LimitOrdersVms.RefreshWeights();
                this.OnTrendLinePitchChanged();
                this.OnTrendLinePitchMessageChanged();
            }
        }
        public bool VmWeightTablesEditIsActive
        {
            get => this.WeightTablesEditIsActive;
            set => this.WeightTablesEditIsActive = value;
        }
        #endregion
    }

}
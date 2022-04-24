﻿using clashN.Base;
using clashN.Handler;
using clashN.Mode;
using clashN.Resx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static clashN.Mode.ClashProviders;
using static clashN.Mode.ClashProxies;

namespace clashN.Forms
{
    public partial class ProxiesForm : BaseForm
    {
        private Dictionary<String, ProxiesItem> proxies;
        private Dictionary<String, ProvidersItem> providers;
        private List<ProxiesItem> lstDetail;
        private int sortColumn = 0;

        //    ProxiesItem selectedProxy;

        #region Init
        public ProxiesForm()
        {
            InitializeComponent();
        }

        private void ProxiesForm_Load(object sender, EventArgs e)
        {
            InitProxiesView();
            InitDetailView();

            GetClashProxies(true);
        }

        private void ProxiesForm_Shown(object sender, EventArgs e)
        {
            lvProxies.Focus();
        }

        #endregion

        #region listview
        private void InitProxiesView()
        {
            lvProxies.BeginUpdate();
            lvProxies.Items.Clear();

            lvProxies.GridLines = true;
            lvProxies.FullRowSelect = true;
            lvProxies.View = View.Details;
            lvProxies.Scrollable = true;
            lvProxies.MultiSelect = false;
            lvProxies.HeaderStyle = ColumnHeaderStyle.Clickable;

            lvProxies.Columns.Add("", 30);
            lvProxies.Columns.Add(ResUI.LvAlias, 130);
            lvProxies.Columns.Add(ResUI.LvServiceType, 80);
            lvProxies.Columns.Add(ResUI.LvActivity, 130);

            lvProxies.EndUpdate();
        }

        private void InitDetailView()
        {
            lvDetail.BeginUpdate();
            lvDetail.Items.Clear();

            lvDetail.GridLines = true;
            lvDetail.FullRowSelect = true;
            lvDetail.View = View.Details;
            lvDetail.Scrollable = true;
            lvDetail.MultiSelect = false;
            lvDetail.HeaderStyle = ColumnHeaderStyle.Clickable;

            lvDetail.Columns.Add("", 30);
            lvDetail.Columns.Add(ResUI.LvAlias, 130);
            lvDetail.Columns.Add(ResUI.LvServiceType, 80);
            lvDetail.Columns.Add(ResUI.LvTestResults, 60, HorizontalAlignment.Right);

            lvDetail.EndUpdate();
        }

        private void lvProxies_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshDetail(GetLvSelectedIndex());
        }

        private void lvProxies_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column < 0)
            {
                return;
            }

            if (e.Column == 0)
            {
                foreach (ColumnHeader it in lvProxies.Columns)
                {
                    it.Width = -2;
                }
                return;
            }
        }

        private void lvDetail_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column < 0)
            {
                return;
            }

            if (e.Column == 0)
            {
                foreach (ColumnHeader it in lvDetail.Columns)
                {
                    it.Width = -2;
                }
                return;
            }

            RefreshDetailSub(e.Column);
        }
        #endregion


        #region  proxy function

        private void GetClashProxies(bool refreshUI)
        {
            MainFormHandler.Instance.GetClashProxies(config, (it, it2) =>
            {
                proxies = it.proxies;
                providers = it2.providers;

                LazyConfig.Instance.SetProxies(proxies);
                if (refreshUI)
                {
                    RefreshProxies();

                    this.BeginInvoke(new Action(() =>
                    {
                        tsbSpeedtest.Enabled = true;
                    }));
                }
            });
        }

        private void RefreshProxies()
        {
            var index = -1;

            lvProxies.BeginInvoke(new Action(() =>
            {
                index = GetLvSelectedIndex();
                lvProxies.BeginUpdate();
                lvProxies.Items.Clear();

                foreach (KeyValuePair<string, ProxiesItem> kv in proxies)
                {
                    if (!Global.allowSelectType.Contains(kv.Value.type.ToLower()))
                    {
                        continue;
                    }
                    ListViewItem lvItem = new ListViewItem("");
                    Utils.AddSubItem(lvItem, "Name", kv.Key);
                    Utils.AddSubItem(lvItem, "Type", kv.Value.type);
                    Utils.AddSubItem(lvItem, "Activity", kv.Value.now);

                    lvProxies.Items.Add(lvItem);
                    lvItem.Tag = kv.Key;
                }
                lvProxies.EndUpdate();

            }));

            lvDetail.BeginInvoke(new Action(() =>
            {
                RefreshDetail(index);
            }));
        }
        private void RefreshDetail(int index)
        {
            //selectedProxy = null;

            lvDetail.BeginUpdate();
            lvDetail.Items.Clear();
            lvDetail.EndUpdate();

            if (index < 0 || index >= lvProxies.Items.Count)
            {
                return;
            }

            var name = lvProxies.Items[index].Tag.ToString();
            if (Utils.IsNullOrEmpty(name))
            {
                return;
            }

            proxies.TryGetValue(name, out ProxiesItem proxy);
            if (proxy == null || proxy.all == null)
            {
                return;
            }
            //selectedProxy = proxy;

            lstDetail = new List<ProxiesItem>();
            foreach (var item in proxy.all)
            {
                var isActive = item == proxy.now;

                var proxy2 = TryGetProxy(item);
                if (proxy2 == null)
                {
                    continue;
                }
                int delay = -1;
                if (proxy2.history.Count > 0)
                {
                    delay = proxy2.history[proxy2.history.Count - 1].delay;
                }
                lstDetail.Add(new ProxiesItem()
                {
                    now = isActive ? Global.CheckMark : "",
                    name = item,
                    type = proxy2.type,
                    delay = delay <= 0 ? 99999999 : delay
                });
            }

            RefreshDetailSub(-1);
        }

        private void RefreshDetailSub(int column)
        {
            if (lstDetail == null)
            {
                return;
            }
            if (column < 0)
            {
                column = sortColumn;
            }
            else
            {
                sortColumn = column;
            }
            lvDetail.BeginUpdate();
            lvDetail.Items.Clear();

            switch (column)
            {
                case 1:
                    lstDetail = lstDetail.OrderBy(x => x.name).ToList();
                    break;
                case 2:
                    lstDetail = lstDetail.OrderBy(x => x.type).ToList();
                    break;
                case 3:
                    lstDetail = lstDetail.OrderBy(x => x.delay).ToList();
                    break;
            }

            foreach (var item in lstDetail)
            {
                ListViewItem lvItem = new ListViewItem(item.now);
                Utils.AddSubItem(lvItem, "Name", item.name);
                Utils.AddSubItem(lvItem, "Type", item.type);
                Utils.AddSubItem(lvItem, "testResult", item.delay >= 99999999 ? "" : item.delay.ToString("#ms"));
                lvItem.Tag = item.name;
                if (item.now == Global.CheckMark)
                {
                    lvItem.ForeColor = Color.DodgerBlue;
                    lvItem.Font = new Font(lvItem.Font, FontStyle.Bold);
                }

                lvDetail.Items.Add(lvItem);
            }

            lvDetail.EndUpdate();
        }

        private ProxiesItem TryGetProxy(string name)
        {
            proxies.TryGetValue(name, out ProxiesItem proxy2);
            if (proxy2 != null)
            {
                return proxy2;
            }
            //from providers
            foreach (KeyValuePair<string, ProvidersItem> kv in providers)
            {
                if (Global.proxyVehicleType.Contains(kv.Value.vehicleType.ToLower()))
                {
                    var proxy3 = kv.Value.proxies.FirstOrDefault(t => t.name == name);
                    if (proxy3 != null)
                    {
                        return proxy3;
                    }
                }
            }
            return null;
        }

        private void lvDetail_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    SetActiveProxy();
                    break;
            }

        }
        private void SetActiveProxy()
        {
            if (lvProxies.SelectedItems == null || lvProxies.SelectedItems.Count <= 0)
            {
                return;
            }
            var name = lvProxies.SelectedItems[0].Tag.ToString();
            if (Utils.IsNullOrEmpty(name))
            {
                return;
            }
            if (lvDetail.SelectedItems == null || lvDetail.SelectedItems.Count <= 0)
            {
                return;
            }
            var nameNode = lvDetail.SelectedItems[0].Tag.ToString();
            if (Utils.IsNullOrEmpty(nameNode))
            {
                return;
            }
            var selectedProxy = TryGetProxy(name);
            if (selectedProxy == null || selectedProxy.type != "Selector")
            {
                UI.Show(ResUI.OperationFailed);
                return;
            }

            var url = $"{Global.httpProtocol}{Global.Loopback}:{config.APIPort}/proxies/{name}";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("name", nameNode);
            _ = HttpClientHelper.GetInstance().PutAsync(url, headers);

            selectedProxy.now = nameNode;
            RefreshDetail(GetLvSelectedIndex());
        }

        #endregion

        #region toolbar

        private void tsbReload_Click(object sender, EventArgs e)
        {
            GetClashProxies(true);
        }

        private void tsbSpeedtest_Click(object sender, EventArgs e)
        {
            if (proxies == null)
            {
                return;
            }
            tsbSpeedtest.Enabled = false;

            MainFormHandler.Instance.ClashProxiesDelayTest(it =>
            {
                GetClashProxies(true);
            });
        }
        private void tsbSelectActivity_Click(object sender, EventArgs e)
        {
            SetActiveProxy();
        }
        private void tsbClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private int GetLvSelectedIndex()
        {
            int index = -1;
            try
            {
                if (lvProxies.SelectedIndices.Count <= 0)
                {
                    return index;
                }

                index = lvProxies.SelectedIndices[0];
                return index;
            }
            catch
            {
                return index;
            }
        }

        #endregion

    }
}

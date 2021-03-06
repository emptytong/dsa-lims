﻿/*	
	DSA Lims - Laboratory Information Management System
    Copyright (C) 2018  Norwegian Radiation Protection Authority

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
// Authors: Dag Robole,

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DSA_lims
{
    public partial class FormOrderNew : Form
    {
        public Guid OrderId = Guid.Empty;
        public string OrderName = String.Empty;

        private Customer mCustomer = null;

        public FormOrderNew()
        {
            InitializeComponent();
        }

        private void FormOrderNew_Load(object sender, EventArgs e)
        {
            SqlConnection conn = null;
            try
            {
                conn = DB.OpenConnection();
                UI.PopulateComboBoxes(conn, "csp_select_laboratories_short", new[] {
                    new SqlParameter("@instance_status_level", InstanceStatus.Active)
                }, cboxLaboratory);

                if(Utils.IsValidGuid(Common.LabId))
                {
                    cboxLaboratory.SelectedValue = Common.LabId;
                }

                cboxRequestedSigma.DataSource = DB.GetSigmaValues(conn, null, false);
                cboxRequestedSigmaMDA.DataSource = DB.GetSigmaValues(conn, null, true);
            }
            catch (Exception ex)
            {
                Common.Log.Error(ex);
                MessageBox.Show(ex.Message);
                DialogResult = DialogResult.Abort;
                Close();
            }
            finally
            {
                conn?.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (!Utils.IsValidGuid(cboxLaboratory.SelectedValue))
            {
                MessageBox.Show("Laboratory is mandatory");
                return;
            }

            if (!Utils.IsValidGuid(cboxResponsible.SelectedValue))
            {
                MessageBox.Show("Responsible is mandatory");
                return;
            }

            if (tbDeadline.Tag == null)
            {
                MessageBox.Show("Deadline is mandatory");
                return;
            }            

            DateTime dl = (DateTime)tbDeadline.Tag;                        
            if(dl.Date < DateTime.Now.Date)
            {
                MessageBox.Show("Deadline can not be in the past");
                return;
            }

            if(mCustomer == null)
            {
                MessageBox.Show("Customer is mandatory");
                return;
            }

            SqlConnection conn = null;
            SqlTransaction trans = null;

            try
            {                
                Guid labId = Utils.MakeGuid(cboxLaboratory.SelectedValue);

                conn = DB.OpenConnection();
                trans = conn.BeginTransaction();
                
                string labPrefix = DB.GetOrderPrefix(conn, trans, labId);
                int orderCount = DB.GetNextOrderCount(conn, trans, labId);
                OrderName = labPrefix + "-" + DateTime.Now.ToString("yyyy") + "-" + orderCount;

                if(DB.NameExists(conn, trans, "assignment", OrderName, Guid.Empty))
                {
                    Common.Log.Error("Order with name " + OrderName + " already exist");
                    MessageBox.Show("Order with name " + OrderName + " already exist");
                    return;
                }
                
                DateTime currDate = DateTime.Now;                
                
                Assignment assignment = new Assignment();                
                assignment.Name = OrderName;
                assignment.LaboratoryId = labId;                
                assignment.AccountId = Utils.MakeGuid(cboxResponsible.SelectedValue);
                assignment.Deadline = (DateTime)tbDeadline.Tag;
                assignment.RequestedSigmaAct = Convert.ToDouble(cboxRequestedSigma.SelectedValue);
                assignment.RequestedSigmaMDA = Convert.ToDouble(cboxRequestedSigmaMDA.SelectedValue);
                assignment.CustomerCompanyName = mCustomer.CompanyName;
                assignment.CustomerCompanyEmail = mCustomer.CompanyEmail;
                assignment.CustomerCompanyPhone = mCustomer.CompanyPhone;
                assignment.CustomerCompanyAddress = mCustomer.CompanyAddress;
                assignment.CustomerContactName = mCustomer.ContactName;
                assignment.CustomerContactEmail = mCustomer.ContactEmail;
                assignment.CustomerContactPhone = mCustomer.ContactPhone;
                assignment.CustomerContactAddress = mCustomer.ContactAddress;
                assignment.WorkflowStatusId = WorkflowStatus.Construction;
                assignment.LastWorkflowStatusDate = currDate;
                assignment.LastWorkflowStatusBy = Common.Username;
                assignment.InstanceStatusId = InstanceStatus.Active;
                assignment.CreateDate = currDate;
                assignment.CreateId = Common.UserId;
                assignment.UpdateDate = currDate;
                assignment.UpdateId = Common.UserId;
                assignment.Description = tbDescription.Text.Trim();

                assignment.StoreToDB(conn, trans);

                string json = JsonConvert.SerializeObject(assignment);
                DB.AddAuditMessage(conn, trans, "assignment", assignment.Id, AuditOperationType.Insert, json, "");

                trans.Commit();

                OrderId = assignment.Id;
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                trans?.Rollback();
                Common.Log.Error(ex);
                MessageBox.Show(ex.Message);
                DialogResult = DialogResult.Abort;
            }
            finally
            {
                conn?.Close();
            }
            
            Close();
        }

        private void cboxLaboratory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Utils.IsValidGuid(cboxLaboratory.SelectedValue))
            {
                cboxResponsible.SelectedValue = Guid.Empty;
                return;
            }

            Guid labId = Utils.MakeGuid(cboxLaboratory.SelectedValue);
            SqlConnection conn = null;
            try
            {
                conn = DB.OpenConnection();
                UI.PopulateComboBoxes(conn, "csp_select_accounts_for_laboratory_short", new[] {
                    new SqlParameter("@laboratory_id", labId),
                    new SqlParameter("@instance_status_level", InstanceStatus.Active)
                }, cboxResponsible);
            }
            catch (Exception ex)
            {
                Common.Log.Error(ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn?.Close();
            }
        }

        private void btnSelectDeadline_Click(object sender, EventArgs e)
        {
            FormSelectDate form = new FormSelectDate();
            if (form.ShowDialog() != DialogResult.OK)
                return;

            DateTime selectedDate = form.SelectedDate;

            if(selectedDate.Date < DateTime.Now.Date)
            {
                MessageBox.Show("Deadline can not be in the past");
                return;
            }

            tbDeadline.Tag = selectedDate;
            tbDeadline.Text = selectedDate.ToString(Utils.DateFormatNorwegian);
        }

        private void btnSelectCustomer_Click(object sender, EventArgs e)
        {
            FormSelectCustomer form = new FormSelectCustomer(InstanceStatus.Active);
            if (form.ShowDialog() != DialogResult.OK)
                return;
            
            mCustomer = form.SelectedCustomer;
            tbCustomer.Text = mCustomer.ContactName;
        }
    }
}

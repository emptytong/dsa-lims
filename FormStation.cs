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

namespace DSA_lims
{
    public partial class FormStation : Form
    {
        ToolTip ttCoords = new ToolTip();

        private Dictionary<string, object> p = new Dictionary<string, object>();

        public Guid StationId
        {
            get { return p.ContainsKey("id") ? (Guid)p["id"] : Guid.Empty; }
        }

        public string StationName
        {
            get { return p.ContainsKey("name") ? p["name"].ToString() : String.Empty; }
        }

        public FormStation()
        {
            InitializeComponent();
            // create new station            
            Text = "Create station";                        
        }
        public FormStation(Guid sid)
        {
            InitializeComponent();
            // edit existing station
            Text = "Update station";
            p["id"] = sid;                        
        }

        private void FormStation_Load(object sender, EventArgs e)
        {            
            tbAltitude.KeyPress += CustomEvents.Numeric_KeyPress;

            string NL = Environment.NewLine;
            ttCoords.SetToolTip(lblLatitude, "Latitude, Longitude" + NL + NL
                + "Formats: " + NL + "61° 34' 12\" N   11° 67' 20\" E" + NL + "61° 34" + Utils.NumberSeparator + "23' N   11° 67" + Utils.NumberSeparator + "33' E"
                + NL + "61" + Utils.NumberSeparator + "543478 N   11" + Utils.NumberSeparator + "776344 E" + NL + "61" + Utils.NumberSeparator + "543478   -11" + Utils.NumberSeparator + "776344" + NL + NL + "° can be replaced with *");

            ttCoords.SetToolTip(lblLongitude, "Latitude, Longitude" + NL + NL
                + "Formats: " + NL + "61° 34' 12\" N   11° 67' 20\" E" + NL + "61° 34" + Utils.NumberSeparator + "23' N   11° 67" + Utils.NumberSeparator + "33' E"
                + NL + "61" + Utils.NumberSeparator + "543478 N   11" + Utils.NumberSeparator + "776344 E" + NL + "61" + Utils.NumberSeparator + "543478   -11" + Utils.NumberSeparator + "776344" + NL + NL + "° can be replaced with *");

            SqlConnection conn = null;
            try
            {
                conn = DB.OpenConnection();

                if (p.ContainsKey("id"))
                {                    
                    cboxInstanceStatus.DataSource = DB.GetIntLemmata(conn, null, "csp_select_instance_status", false);

                    SqlCommand cmd = new SqlCommand("csp_select_station", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", p["id"]);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            throw new Exception("Station with ID " + p["id"] + " was not found");

                        reader.Read();

                        tbName.Text = reader.GetString("name");
                        tbLatitude.Text = reader.GetString("latitude");
                        tbLongitude.Text = reader.GetString("longitude");
                        tbAltitude.Text = reader.GetString("altitude");
                        cboxInstanceStatus.SelectedValue = reader.GetInt32("instance_status_id");
                        tbComment.Text = reader.GetString("comment");
                        p["create_date"] = reader["create_date"];
                        p["create_id"] = reader["create_id"];
                        p["update_date"] = reader["update_date"];
                        p["update_id"] = reader["update_id"];
                    }                
                }
                else
                {                    
                    cboxInstanceStatus.DataSource = DB.GetIntLemmata(conn, null, "csp_select_instance_status", false);                
                    cboxInstanceStatus.SelectedValue = InstanceStatus.Active;
                }
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

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(tbName.Text.Trim()))
            {
                MessageBox.Show("Name is mandatory");
                return;
            }

            if (String.IsNullOrEmpty(tbLatitude.Text.Trim()))
            {
                MessageBox.Show("Latitude is mandatory");
                return;
            }

            if (String.IsNullOrEmpty(tbLongitude.Text.Trim()))
            {
                MessageBox.Show("Longitude is mandatory");
                return;
            }

            try
            {
                p["latitude"] = UtilsGeo.GetLatitude(tbLatitude.Text);
                p["longitude"] = UtilsGeo.GetLongitude(tbLongitude.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }            

            p["name"] = tbName.Text.Trim();
            if (String.IsNullOrEmpty(tbAltitude.Text))
                p["altitude"] = DBNull.Value;
            else p["altitude"] = Convert.ToDouble(tbAltitude.Text);
            p["instance_status_id"] = cboxInstanceStatus.SelectedValue;
            p["comment"] = tbComment.Text.Trim();

            SqlConnection connection = null;
            SqlTransaction transaction = null;
            bool success = true;

            try
            {
                connection = DB.OpenConnection();
                transaction = connection.BeginTransaction();

                if (DB.NameExists(connection, transaction, "station", p["name"].ToString(), StationId))
                {
                    MessageBox.Show("The station '" + p["name"] + "' already exists");
                    return;
                }

                if (!p.ContainsKey("id"))
                    InsertStation(connection, transaction);
                else
                    UpdateStation(connection, transaction);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                success = false;
                transaction?.Rollback();
                Common.Log.Error(ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection?.Close();
            }

            DialogResult = success ? DialogResult.OK : DialogResult.Abort;
            Close();
        }

        private void InsertStation(SqlConnection conn, SqlTransaction trans)
        {            
            p["create_date"] = DateTime.Now;
            p["create_id"] = Common.UserId;
            p["update_date"] = DateTime.Now;
            p["update_id"] = Common.UserId;        

            SqlCommand cmd = new SqlCommand("csp_insert_station", conn, trans);
            cmd.CommandType = CommandType.StoredProcedure;
            p["id"] = Guid.NewGuid();
            cmd.Parameters.AddWithValue("@id", p["id"]);
            cmd.Parameters.AddWithValue("@name", p["name"]);
            cmd.Parameters.AddWithValue("@latitude", p["latitude"]);
            cmd.Parameters.AddWithValue("@longitude", p["longitude"]);
            cmd.Parameters.AddWithValue("@altitude", p["altitude"]);
            cmd.Parameters.AddWithValue("@instance_status_id", p["instance_status_id"]);
            cmd.Parameters.AddWithValue("@comment", p["comment"], String.Empty);
            cmd.Parameters.AddWithValue("@create_date", p["create_date"]);
            cmd.Parameters.AddWithValue("@create_id", p["create_id"]);
            cmd.Parameters.AddWithValue("@update_date", p["update_date"]);
            cmd.Parameters.AddWithValue("@update_id", p["update_id"]);
            cmd.ExecuteNonQuery();
        }

        private void UpdateStation(SqlConnection conn, SqlTransaction trans)
        {            
            p["update_date"] = DateTime.Now;
            p["update_id"] = Common.UserId;

            SqlCommand cmd = new SqlCommand("csp_update_station", conn, trans);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@id", p["id"]);
            cmd.Parameters.AddWithValue("@name", p["name"]);
            cmd.Parameters.AddWithValue("@latitude", p["latitude"]);
            cmd.Parameters.AddWithValue("@longitude", p["longitude"]);
            cmd.Parameters.AddWithValue("@altitude", p["altitude"]);
            cmd.Parameters.AddWithValue("@instance_status_id", p["instance_status_id"]);
            cmd.Parameters.AddWithValue("@comment", p["comment"], String.Empty);
            cmd.Parameters.AddWithValue("@update_date", p["update_date"]);
            cmd.Parameters.AddWithValue("@update_id", p["update_id"]);
            cmd.ExecuteNonQuery();
        }

        private void btnSelectCoordsFromMap_Click(object sender, EventArgs e)
        {
            double? lat = null, lon = null;
            
            if(!String.IsNullOrEmpty(tbLatitude.Text) && !String.IsNullOrEmpty(tbLongitude.Text))
            {
                try
                {
                    lat = Convert.ToDouble(tbLatitude.Text);
                    lon = Convert.ToDouble(tbLongitude.Text);
                }
                catch { }
            }

            FormGetCoords form = new FormGetCoords(lat, lon);
            if (form.ShowDialog() != DialogResult.OK)
                return;

            tbLatitude.Text = form.SelectedLatitude.ToString();
            tbLongitude.Text = form.SelectedLongitude.ToString();
        }
    }
}

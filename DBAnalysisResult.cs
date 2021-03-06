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
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DSA_lims
{
    [JsonObject]
    public class AnalysisResult
    {
        public AnalysisResult()
        {
            Id = Guid.NewGuid();
            Dirty = false;
            InstanceStatusId = InstanceStatus.Active;
        }

        public Guid Id { get; set; }
        public Guid AnalysisId { get; set; }
        public Guid NuclideId { get; set; }
        public string NuclideName { get; set; }
        public double? Activity { get; set; }
        public double? ActivityUncertaintyABS { get; set; }
        public bool ActivityApproved { get; set; }
        public double? UniformActivity { get; set; }
        public int UniformActivityUnitId { get; set; }
        public double? DetectionLimit { get; set; }
        public bool DetectionLimitApproved { get; set; }
        public bool Accredited { get; set; }
        public bool Reportable { get; set; }
        public int InstanceStatusId { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreateId { get; set; }
        public DateTime UpdateDate { get; set; }
        public Guid UpdateId { get; set; }

        public bool Dirty;

        public bool IsDirty()
        {
            return Dirty;
        }

        public void ClearDirty()
        {
            Dirty = false;
        }

        public string GetNuclideName(SqlConnection conn, SqlTransaction trans)
        {
            object o = DB.GetScalar(conn, trans, "select name from nuclide where id = @nid", CommandType.Text, new SqlParameter("@nid", NuclideId));
            return !DB.IsValidField(o) ? "" : o.ToString();
        }

        public static bool IdExists(SqlConnection conn, SqlTransaction trans, Guid analResId)
        {
            int cnt = (int)DB.GetScalar(conn, trans, "select count(*) from analysis_result where id = @id", CommandType.Text, new SqlParameter("@id", analResId));
            return cnt > 0;
        }

        public void LoadFromDB(SqlConnection conn, SqlTransaction trans, Guid analResId)
        {
            using (SqlDataReader reader = DB.GetDataReader(conn, trans, "csp_select_analysis_result", CommandType.StoredProcedure,
                new SqlParameter("@id", analResId)))
            {
                if (!reader.HasRows)
                    throw new Exception("Error: Analysis result with id " + analResId.ToString() + " was not found");

                reader.Read();
                                        
                Id = reader.GetGuid("id");
                AnalysisId = reader.GetGuid("analysis_id");
                NuclideId = reader.GetGuid("nuclide_id");
                Activity = reader.GetDoubleNullable("activity");
                ActivityUncertaintyABS = reader.GetDoubleNullable("activity_uncertainty_abs");
                ActivityApproved = reader.GetBoolean("activity_approved");
                UniformActivity = reader.GetDoubleNullable("uniform_activity");
                UniformActivityUnitId = reader.GetInt32("uniform_activity_unit_id");
                DetectionLimit = reader.GetDoubleNullable("detection_limit");
                DetectionLimitApproved = reader.GetBoolean("detection_limit_approved");
                Accredited = reader.GetBoolean("accredited");
                Reportable = reader.GetBoolean("reportable");
                InstanceStatusId = reader.GetInt32("instance_status_id");
                CreateDate = reader.GetDateTime("create_date");
                CreateId = reader.GetGuid("create_id");
                UpdateDate = reader.GetDateTime("update_date");
                UpdateId = reader.GetGuid("update_id");
            }

            NuclideName = GetNuclideName(conn, trans);

            Dirty = false;
        }

        public void StoreToDB(SqlConnection conn, SqlTransaction trans)
        {
            if (Id == Guid.Empty)
                throw new Exception("Error: Can not store an analysis result with empty id");

            SqlCommand cmd = new SqlCommand("", conn, trans);

            if (!AnalysisResult.IdExists(conn, trans, Id))
            {
                // insert new analysis result
                cmd.CommandText = "csp_insert_analysis_result";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", Id);
                cmd.Parameters.AddWithValue("@analysis_id", AnalysisId, Guid.Empty);
                cmd.Parameters.AddWithValue("@nuclide_id", NuclideId, Guid.Empty);
                cmd.Parameters.AddWithValue("@activity", Activity, null);
                cmd.Parameters.AddWithValue("@activity_uncertainty_abs", ActivityUncertaintyABS, null);
                cmd.Parameters.AddWithValue("@activity_approved", ActivityApproved);
                // FIXME
                /*double uAct = -1.0;
                int uActUnitId = -1;
                if (Utils.IsValidGuid(ActivityUnitId))
                {
                    DB.GetUniformActivity(conn, trans, r.Activity, ActivityUnitId, out uAct, out uActUnitId);
                }*/
                cmd.Parameters.AddWithValue("@uniform_activity", DBNull.Value);
                cmd.Parameters.AddWithValue("@uniform_activity_unit_id", 0);
                cmd.Parameters.AddWithValue("@detection_limit", DetectionLimit, null);
                cmd.Parameters.AddWithValue("@detection_limit_approved", DetectionLimitApproved);
                cmd.Parameters.AddWithValue("@accredited", Accredited);
                cmd.Parameters.AddWithValue("@reportable", Reportable);
                cmd.Parameters.AddWithValue("@instance_status_id", InstanceStatusId);
                cmd.Parameters.AddWithValue("@create_date", DateTime.Now);
                cmd.Parameters.AddWithValue("@create_id", Common.UserId, Guid.Empty);
                cmd.Parameters.AddWithValue("@update_date", DateTime.Now);
                cmd.Parameters.AddWithValue("@update_id", Common.UserId, Guid.Empty);

                cmd.ExecuteNonQuery();

                Dirty = false;
            }
            else
            {
                if(Dirty)
                {
                    // update existing analysis result
                    cmd.CommandText = "csp_update_analysis_result";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@id", Id);
                    cmd.Parameters.AddWithValue("@activity", Activity, null);
                    cmd.Parameters.AddWithValue("@activity_uncertainty_abs", ActivityUncertaintyABS, null);
                    cmd.Parameters.AddWithValue("@activity_approved", ActivityApproved, null);
                    // FIXME
                    /*double uAct = -1.0;
                    int uActUnitId = -1;
                    if (Utils.IsValidGuid(ActivityUnitId))
                    {
                        DB.GetUniformActivity(conn, trans, r.Activity, ActivityUnitId, out uAct, out uActUnitId);
                    }*/
                    cmd.Parameters.AddWithValue("@uniform_activity", DBNull.Value);
                    cmd.Parameters.AddWithValue("@uniform_activity_unit_id", 0);
                    cmd.Parameters.AddWithValue("@detection_limit", DetectionLimit, null);
                    cmd.Parameters.AddWithValue("@detection_limit_approved", DetectionLimitApproved);
                    cmd.Parameters.AddWithValue("@accredited", Accredited);
                    cmd.Parameters.AddWithValue("@reportable", Reportable);
                    cmd.Parameters.AddWithValue("@instance_status_id", InstanceStatusId);
                    cmd.Parameters.AddWithValue("@update_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@update_id", Common.UserId, Guid.Empty);

                    cmd.ExecuteNonQuery();

                    Dirty = false;
                }
            }
        }
    }
}

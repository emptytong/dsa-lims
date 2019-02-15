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

namespace DSA_lims
{
    public class PreparationGeometry
    {
        public PreparationGeometry()
        {
        }

        public PreparationGeometry(SqlConnection conn, SqlTransaction trans, Guid id)
        {
            using (SqlDataReader reader = DB.GetDataReader(conn, trans, "csp_select_preparation_geometry", CommandType.StoredProcedure,
                    new SqlParameter("@id", id)))
            {
                if (reader.HasRows)
                {
                    reader.Read();

                    Id = reader.GetGuid("id");
                    Name = reader.GetString("name");
                    MinFillHeightMM = reader.GetDoubleNullable("min_fill_height_mm");
                    MaxFillHeightMM = reader.GetDoubleNullable("max_fill_height_mm");
                    InstanceStatusId = reader.GetInt32Nullable("instance_status_id");
                    Comment = reader.GetString("comment");
                    CreateDate = reader.GetDateTimeNullable("create_date");
                    CreatedBy = reader.GetString("created_by");
                    UpdateDate = reader.GetDateTimeNullable("update_date");
                    UpdatedBy = reader.GetString("updated_by");
                }
            }
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public double? MinFillHeightMM { get; set; }
        public double? MaxFillHeightMM { get; set; }
        public int? InstanceStatusId { get; set; }
        public string Comment { get; set; }
        public DateTime? CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}

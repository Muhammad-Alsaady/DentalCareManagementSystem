using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalCareManagmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class last : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "TreatmentPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId1",
                table: "TreatmentPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "TreatmentItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "PaymentTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "PatientImages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "NotificationLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "DiagnosisNotes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PatientAppointmentId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientAppointmentId",
                table: "TreatmentPlans",
                column: "PatientAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientAppointmentId1",
                table: "TreatmentPlans",
                column: "PatientAppointmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentItems_PatientAppointmentId",
                table: "TreatmentItems",
                column: "PatientAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PatientAppointmentId",
                table: "PaymentTransactions",
                column: "PatientAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientImages_PatientAppointmentId",
                table: "PatientImages",
                column: "PatientAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_PatientAppointmentId",
                table: "NotificationLogs",
                column: "PatientAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisNotes_PatientAppointmentId",
                table: "DiagnosisNotes",
                column: "PatientAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PatientAppointmentId",
                table: "AuditLogs",
                column: "PatientAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_PatientAppointments_PatientAppointmentId",
                table: "AuditLogs",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiagnosisNotes_PatientAppointments_PatientAppointmentId",
                table: "DiagnosisNotes",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLogs_PatientAppointments_PatientAppointmentId",
                table: "NotificationLogs",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientImages_PatientAppointments_PatientAppointmentId",
                table: "PatientImages",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_PatientAppointments_PatientAppointmentId",
                table: "PaymentTransactions",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentItems_PatientAppointments_PatientAppointmentId",
                table: "TreatmentItems",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_PatientAppointments_PatientAppointmentId",
                table: "TreatmentPlans",
                column: "PatientAppointmentId",
                principalTable: "PatientAppointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_PatientAppointments_PatientAppointmentId1",
                table: "TreatmentPlans",
                column: "PatientAppointmentId1",
                principalTable: "PatientAppointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_PatientAppointments_PatientAppointmentId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DiagnosisNotes_PatientAppointments_PatientAppointmentId",
                table: "DiagnosisNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationLogs_PatientAppointments_PatientAppointmentId",
                table: "NotificationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientImages_PatientAppointments_PatientAppointmentId",
                table: "PatientImages");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_PatientAppointments_PatientAppointmentId",
                table: "PaymentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentItems_PatientAppointments_PatientAppointmentId",
                table: "TreatmentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_PatientAppointments_PatientAppointmentId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_PatientAppointments_PatientAppointmentId1",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_PatientAppointmentId",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_PatientAppointmentId1",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentItems_PatientAppointmentId",
                table: "TreatmentItems");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_PatientAppointmentId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PatientImages_PatientAppointmentId",
                table: "PatientImages");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_PatientAppointmentId",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_DiagnosisNotes_PatientAppointmentId",
                table: "DiagnosisNotes");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PatientAppointmentId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId1",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "TreatmentItems");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "PatientImages");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "DiagnosisNotes");

            migrationBuilder.DropColumn(
                name: "PatientAppointmentId",
                table: "AuditLogs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eiupdds_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDiseaseIdFromPredictionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PredictionItems_Diseases_DiseaseId",
                table: "PredictionItems");

            migrationBuilder.DropIndex(
                name: "IX_PredictionItems_DiseaseId",
                table: "PredictionItems");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "PredictionItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiseaseId",
                table: "PredictionItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionItems_DiseaseId",
                table: "PredictionItems",
                column: "DiseaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PredictionItems_Diseases_DiseaseId",
                table: "PredictionItems",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

using ex1.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ex1.Migrations
{
    /// <summary>
    /// Adds the SQL Server objects behind the raffle: a ticket-count function, an atomic
    /// winner-recording procedure, and a sales report procedure. No table changes.
    /// </summary>
    [DbContext(typeof(StoreContext))]
    [Migration("20261010120000_AddRaffleStoredProcedures")]
    public partial class AddRaffleStoredProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Paid tickets for a prize. CartStatus.Paid is stored as 1.
            migrationBuilder.Sql(@"
CREATE OR ALTER FUNCTION dbo.fn_PrizeTicketCount (@PrizeId INT)
RETURNS INT
AS
BEGIN
    RETURN (
        SELECT ISNULL(SUM(ci.Quantity), 0)
        FROM dbo.CartItems ci
        INNER JOIN dbo.Carts c ON c.Id = ci.CartId
        WHERE ci.PrizeId = @PrizeId AND c.Status = 1
    );
END");

            // The WHERE IsRaffleDone = 0 guard makes the check-and-set a single atomic statement:
            // a concurrent second call blocks on the row lock, re-checks, and updates nothing.
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_RecordWinner
    @PrizeId INT,
    @WinnerUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Prizes
    SET WinnerUserId = @WinnerUserId,
        IsRaffleDone = 1
    WHERE Id = @PrizeId
      AND IsRaffleDone = 0;

    SELECT @@ROWCOUNT AS [Value];
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetRaffleReport
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id AS PrizeId,
        p.Name AS PrizeName,
        t.TicketsSold,
        CAST(t.TicketsSold * p.TicketPrice AS DECIMAL(18, 2)) AS Revenue,
        p.IsRaffleDone,
        p.WinnerUserId,
        CASE WHEN u.Id IS NULL THEN NULL ELSE u.FirstName + ' ' + u.LastName END AS WinnerName
    FROM dbo.Prizes p
    CROSS APPLY (SELECT dbo.fn_PrizeTicketCount(p.Id) AS TicketsSold) t
    LEFT JOIN dbo.Users u ON u.Id = p.WinnerUserId
    ORDER BY Revenue DESC, p.Id;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetRaffleReport;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_RecordWinner;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_PrizeTicketCount;");
        }
    }
}

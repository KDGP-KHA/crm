import pyodbc

conn = pyodbc.connect('DRIVER={ODBC Driver 17 for SQL Server};SERVER=10.57.30.10;DATABASE=quanlydoanhthucenit;UID=testuser;PWD=123456;TrustServerCertificate=yes')
cursor = conn.cursor()
cursor.execute("SELECT definition FROM sys.sql_modules WHERE object_id = OBJECT_ID('dbo.RM_DigitalSalesTracking_UpdateStatus')")
row = cursor.fetchone()
if row:
    print(row[0])

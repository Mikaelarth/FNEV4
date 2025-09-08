import sqlite3
import os

db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
print(f"Testing database access: {db_path}")

if os.path.exists(db_path):
    print("✅ Database file exists")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        cursor.execute("SELECT COUNT(*) FROM sqlite_master WHERE type='table'")
        table_count = cursor.fetchone()[0]
        print(f"✅ Tables found: {table_count}")
        
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall()]
        print(f"✅ Table names: {', '.join(tables)}")
        
        # Test clients table if exists
        if 'Clients' in tables:
            cursor.execute("SELECT COUNT(*) FROM Clients")
            client_count = cursor.fetchone()[0]
            print(f"✅ Clients count: {client_count}")
        
        conn.close()
        print("✅ Database access successful - CENTRALIZED SYSTEM WORKING!")
        
    except Exception as e:
        print(f"❌ Database access error: {e}")
else:
    print("❌ Database file not found")

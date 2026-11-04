-- Issue #11: Savings Goals
-- Chạy script này một lần trong Supabase SQL Editor trước khi chạy ứng dụng.

CREATE TABLE IF NOT EXISTS "Goals"
(
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "TargetAmount" NUMERIC(18, 2) NOT NULL CHECK ("TargetAmount" > 0),
    "CurrentAmount" NUMERIC(18, 2) NOT NULL DEFAULT 0 CHECK ("CurrentAmount" >= 0),
    "TargetDate" DATE NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Đảm bảo mỗi người dùng chỉ truy cập các mục tiêu của chính họ.
CREATE INDEX IF NOT EXISTS "IX_Goals_UserId"
    ON "Goals" ("UserId");

-- Liên kết Goals với AspNetUsers nếu bảng người dùng đã tồn tại.
-- UserId được khai báo VARCHAR(255), tương thích với khóa Identity dạng chuỗi.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_Goals_AspNetUsers_UserId'
    ) THEN
        ALTER TABLE "Goals"
        ADD CONSTRAINT "FK_Goals_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id")
        ON DELETE CASCADE;
    END IF;
END $$;

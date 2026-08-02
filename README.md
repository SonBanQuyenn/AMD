# Poll & Survey Builder — microservice solution (AMD201)

Kiến trúc: `API Gateway` (Ocelot) đứng trước `Poll service` + `Vote service` +
`Realtime service` (SignalR), mỗi service độc lập, giao tiếp qua REST. Xem chi tiết
trong phần "Kiến trúc" bên dưới.

## Trạng thái hiện tại

- ✅ **Poll service** — chạy được đầy đủ: tạo poll, xem poll, đóng poll, endpoint
  `/status` cho service khác gọi vào. Controller: `Controllers/PollsController.cs`.
- ✅ **Vote service** — chạy được đầy đủ: nhận vote, gọi sang Poll service để validate
  (`PollServiceClient`), chặn vote trùng, trả kết quả, gọi Realtime service để broadcast.
  Controller: `Controllers/VotesController.cs`.
- ✅ **Realtime service** — chạy được đầy đủ: SignalR hub (`ResultsHub`), client join theo
  poll code, `Controllers/BroadcastController.cs` nhận từ Vote service rồi đẩy xuống mọi
  client đang xem poll đó.
- ✅ **API Gateway** — Ocelot, route `/api/polls/**` → Poll service, `/api/votes/**` →
  Vote service, `/hubs/**` → Realtime service (SignalR qua gateway là tính năng thử
  nghiệm của Ocelot - xem ghi chú bên dưới).
- ✅ **Frontend** — React (Vite), 3 trang: tạo poll, vote, xem kết quả trực tiếp qua SignalR.

Tất cả 4 project backend đều theo cấu trúc **Controller-based Web API** (giống template "ASP.NET
Core Web API" mặc định của Visual Studio: `Controllers/`, `[ApiController]`, Swagger),
không dùng Minimal API. Database dùng **SQL Server** (LocalDB cho local dev, container
cho Docker) - xem phần "Database" bên dưới.

## Chạy Frontend

```bash
cd frontend
npm install
npm run dev
```

Mở `http://localhost:5173`. File `.env` đã trỏ sẵn:
- `VITE_GATEWAY_URL=http://localhost:5000/api` — mọi request REST đi qua Gateway.
- `VITE_REALTIME_URL=http://localhost:5003` — SignalR nối **thẳng** tới Realtime service
  (không qua gateway, xem lý do ở phần "Test gateway" bên dưới).

Cần chạy đủ cả 4 service backend (F5 trong Visual Studio với multiple startup projects,
hoặc `docker compose up --build`) trước khi mở frontend.

**3 trang:**
- `/` — tạo poll (có thể chọn thêm thời gian hết hạn tùy chọn), tạo xong tự động
  chuyển sang trang phân tích (`/poll/:code/results`) của chính poll đó.
- `/poll/:code` — trang vote, chọn 1 lựa chọn để gửi phiếu (chặn vote trùng bằng
  token lưu ở `localStorage`, xem `getVoterToken()` trong `src/api.js`). Vote bị
  chặn nếu poll đã bị người tạo dừng thủ công hoặc đã quá `ExpiresAt`.
- `/poll/:code/results` — **trang phân tích, chỉ người tạo poll xem được.** Hiện
  kết quả trực tiếp qua SignalR, link để chia sẻ cho người vote, và nút "Dừng
  nhận phiếu ngay" để đóng poll trước hạn bất cứ lúc nào.

**Cơ chế "chỉ người tạo mới xem được trang phân tích":** không có tài khoản/đăng
nhập trong scope bài này, nên dùng một `CreatorToken` bí mật: Poll service sinh
token này lúc `POST /polls` và **chỉ trả về đúng 1 lần** trong response tạo poll.
Frontend lưu token vào `localStorage` (`saveCreatorToken` trong `src/api.js`).
Mọi lần sau, `GET /polls/{code}?creatorToken=...` và `POST /polls/{code}/close`
đều cần gửi token này; token khớp thì mới được xem trang phân tích / dừng poll.
Vì token nằm trong `localStorage` của trình duyệt, chỉ trình duyệt đã tạo poll
mới có quyền này - người khác chỉ có link vote thì không xem được trang phân tích.

**Đóng poll trước hạn / tự hết hạn:**
- `Poll.ExpiresAt` (tùy chọn) là mốc thời gian tự động đóng, cấu hình lúc tạo poll.
- `PollsController.Get`/`GetStatus` tự tính `IsExpired` mỗi lần gọi (so với
  `DateTime.UtcNow`) - không cần job nền, poll tự "đóng" ngay khi hết hạn.
- Người tạo có thể bấm "Dừng nhận phiếu ngay" ở trang phân tích để đóng poll thủ
  công bất kỳ lúc nào, kể cả trước khi tới `ExpiresAt` (gọi `POST /polls/{code}/close`
  kèm `CreatorToken`, trả 403 nếu token không khớp).

## Database: SQL Server (LocalDB cho local dev, container cho Docker)

- **Local dev (Visual Studio)**: dùng **LocalDB** — đã có sẵn khi cài Visual Studio, không cần
  cài hay chạy gì thêm. Connection string trong `appsettings.json` của từng service đã trỏ
  sẵn tới `(localdb)\mssqllocaldb`.
- **Docker Compose**: dùng container `mcr.microsoft.com/mssql/server:2022-latest`, connection
  string được override qua biến môi trường trong `docker-compose.yml`.
- Poll service dùng DB `PollSurveyDb`, Vote service dùng DB `PollSurveyVoteDb` — vẫn đúng
  nguyên tắc mỗi microservice sở hữu DB riêng, dù chạy chung 1 SQL Server instance.
- Databases **tự động được tạo** khi chạy `dotnet ef database update` (khác Postgres, SQL
  Server tự tạo DB nếu chưa tồn tại) - không cần script init riêng.

## Xem dữ liệu qua Server Explorer (Visual Studio)

1. **View → Server Explorer** → chuột phải **Data Connections** → **Add Connection**.
2. Data source: **Microsoft SQL Server**. Server name: `(localdb)\mssqllocaldb`.
3. Chọn database `PollSurveyDb` (tạo thêm 1 connection nữa cho `PollSurveyVoteDb`).
4. Mở rộng cây → **Tables** → double-click bảng để xem/sửa dữ liệu trực tiếp.

## Tạo migration lần đầu (bắt buộc trước khi chạy service)

```bash
cd src/PollService.Api
dotnet ef migrations add InitialCreate --context PollDbContext --output-dir Migrations
dotnet ef database update --context PollDbContext

cd ../VoteService.Api
dotnet ef migrations add InitialCreate --context VoteDbContext --output-dir Migrations
dotnet ef database update --context VoteDbContext
```

Không cần Docker/Postgres chạy cho bước này - LocalDB tự khởi động khi cần.

> Nếu bạn đã từng chạy migration `InitialCreate` cho `PollDbContext` trước khi có
> cột `CreatorToken`, chạy thêm một migration mới rồi update lại database:
> ```bash
> cd src/PollService.Api
> dotnet ef migrations add AddCreatorToken --context PollDbContext --output-dir Migrations
> dotnet ef database update --context PollDbContext
> ```

## Mở project trong Visual Studio

1. Mở file `PollSurveyApp.sln`.
2. Visual Studio sẽ tự restore NuGet packages. Nếu không, chuột phải vào Solution → **Restore NuGet Packages**.
3. Chạy migration như trên (chỉ cần làm 1 lần, hoặc mỗi khi đổi entity).
4. Chuột phải `PollService.Api` → **Set as Startup Project**, F5. Swagger UI mở tự động tại `/swagger`.

## Test nhanh bằng curl (hoặc dùng Swagger UI)

```bash
# Tạo poll
curl -X POST http://localhost:5001/polls \
  -H "Content-Type: application/json" \
  -d '{"question":"Ngôn ngữ bạn thích nhất?","options":["C#","Python","JavaScript"]}'

# Xem poll (thay YOUR_CODE bằng code trả về ở bước trên).
# response tạo poll ở trên có "creatorToken" - CHỈ xuất hiện đúng 1 lần lúc tạo.
curl http://localhost:5001/polls/YOUR_CODE

# Xem poll KÈM quyền chủ (isCreator=true nếu creatorToken khớp)
curl "http://localhost:5001/polls/YOUR_CODE?creatorToken=YOUR_CREATOR_TOKEN"

# Endpoint dành cho service khác gọi (Vote service sẽ dùng cái này)
curl http://localhost:5001/polls/YOUR_CODE/status

# Dừng poll trước hạn - cần đúng creatorToken, sai/thiếu sẽ trả 403
curl -X POST http://localhost:5001/polls/YOUR_CODE/close \
  -H "Content-Type: application/json" \
  -d '{"creatorToken":"YOUR_CREATOR_TOKEN"}'
```

## Chạy Vote service (local, không dùng Docker)

Vote service cần **Poll service đang chạy** (để gọi `/status`). DB `PollSurveyVoteDb` đã
tự tạo ở bước migration.

1. Chạy `PollService.Api` trước (F5, hoặc `dotnet run` — cổng 5001).
2. Chuột phải `VoteService.Api` → **Set as Startup Project**, F5 (cổng 5002).

Test nhanh:
```bash
# Tạo poll trước (xem ví dụ Poll service ở trên), lấy YOUR_CODE

# Vote
curl -X POST http://localhost:5002/votes \
  -H "Content-Type: application/json" \
  -d '{"pollCode":"YOUR_CODE","optionIndex":0,"voterToken":"test-voter-1"}'

# Vote lần 2 với cùng voterToken -> sẽ bị 409 Conflict (chặn vote trùng)
curl -X POST http://localhost:5002/votes \
  -H "Content-Type: application/json" \
  -d '{"pollCode":"YOUR_CODE","optionIndex":1,"voterToken":"test-voter-1"}'

# Xem kết quả
curl http://localhost:5002/votes/YOUR_CODE/results
```

## Test end-to-end cả 3 service (không cần frontend)

Chạy cả 3 service (F5 lần lượt 3 project trong Visual Studio với "Multiple startup
projects", hoặc đơn giản nhất là `docker compose up --build`), rồi:

```bash
# 1. Tạo poll qua Poll service
curl -X POST http://localhost:5001/polls \
  -H "Content-Type: application/json" \
  -d '{"question":"Ngôn ngữ bạn thích nhất?","options":["C#","Python","JavaScript"]}'
# -> lấy "code" trong response, gọi là YOUR_CODE

# 2. Vote qua Vote service - Vote service sẽ tự gọi sang Poll service để check,
#    rồi gọi sang Realtime service để broadcast
curl -X POST http://localhost:5002/votes \
  -H "Content-Type: application/json" \
  -d '{"pollCode":"YOUR_CODE","optionIndex":0,"voterToken":"test-voter-1"}'

# 3. Xem log console của Realtime service - nếu thấy request POST /broadcast/YOUR_CODE
#    tới, nghĩa là 3 service đã nói chuyện được với nhau thành công.
```

Muốn xem trực quan hơn (không chỉ qua log), mở Postman/console JS bất kỳ, kết nối
`http://localhost:5003/hubs/results` bằng thư viện `@microsoft/signalr`, gọi
`connection.invoke("JoinPoll", "YOUR_CODE")`, rồi lặp lại bước vote ở trên - sẽ thấy
event `ResultsUpdated` bắn về ngay lập tức.

## Test gateway (Ocelot)

Chạy đủ 4 project (hoặc `docker compose up --build`), rồi gọi qua gateway thay vì
gọi thẳng từng service:

```bash
curl -X POST http://localhost:5000/api/polls \
  -H "Content-Type: application/json" \
  -d '{"question":"Test qua gateway?","options":["Có","Không"]}'
```

Kết quả phải giống hệt gọi thẳng `localhost:5001/polls`. SignalR qua gateway
(`ws://localhost:5000/hubs/results`) là tính năng Ocelot đánh dấu thử nghiệm - nếu
gặp lỗi khi làm frontend, cho frontend kết nối thẳng `ws://localhost:5003/hubs/results`
thay vì qua gateway (vẫn đúng kiến trúc, chỉ bỏ qua gateway riêng cho giao thức này).

## Chạy toàn bộ bằng Docker Compose (1 lệnh cho cả backend lẫn frontend)

```bash
docker compose up --build
```

Lệnh này khởi động **tất cả**: SQL Server, 4 service backend (Poll/Vote/Realtime/Gateway),
và cả frontend (chạy Vite dev server trong container, có mount code nên sửa file
trên máy vẫn tự hot-reload như chạy `npm run dev` bình thường).

- Frontend: `http://localhost:5173`
- Gateway: `http://localhost:5000`, Poll service: `http://localhost:5001` (vẫn gọi
  trực tiếp được để debug). SQL Server container chạy ở port `1433`.

Lưu ý: SQL Server khởi động chậm hơn các service .NET vài giây. Các service backend
đã có `restart: on-failure` nên nếu container nào crash ở lần thử đầu (do SQL Server
chưa kịp sẵn sàng), Docker sẽ tự khởi động lại - không cần bạn làm gì thêm, chỉ cần
đợi khoảng 10-20 giây sau `docker compose up` là mọi thứ ổn định.

Muốn dừng toàn bộ: `Ctrl+C` rồi `docker compose down` (thêm `-v` nếu muốn xoá luôn
dữ liệu SQL Server đã lưu).

## Kiến trúc

```
Frontend SPA (chưa code)
   |-- mọi request REST --> API Gateway (Ocelot, port 5000)
   |-- WebSocket (khuyến nghị) --> Realtime service trực tiếp (port 5003)

API Gateway
   |-- /api/polls/**  --> Poll service      (own poll DB - port 5001)
   |-- /api/votes/**  --> Vote service      (own vote DB - port 5002)
   |-- /hubs/**       --> Realtime service  (SignalR hub - port 5003, thử nghiệm)

Vote service
   |-- GET  /polls/{code}/status   --> Poll service      (validate trước khi ghi vote)
   |-- POST /broadcast/{code}      --> Realtime service  (best-effort, sau khi ghi vote)
```

Vote service không tự validate poll — nó luôn hỏi Poll service qua REST trước khi
ghi vote. Đây là điểm quan trọng nhất để giải thích trong phần presentation: ranh
giới trách nhiệm rõ ràng + giao tiếp service-to-service thật, không phải chỉ tách
code cho có.

## Việc tiếp theo (theo đúng thứ tự)

1. ~~**Vote service**~~ ✅ xong.
2. ~~**Realtime service**~~ ✅ xong.
3. ~~**API Gateway**~~ ✅ xong.
4. ~~**EF Migrations**~~ ✅ xong (SQL Server + `Database.Migrate()`).
5. ~~**Frontend**~~ ✅ xong (React + Vite).
6. **Unit test** cho logic validate (question rỗng, số option ngoài khoảng 2–6, vote
   trùng, poll hết hạn, gọi status khi poll không tồn tại).
7. **GitHub Actions**: build + push 4 image (Poll, Vote, Realtime, Gateway), deploy lên
   Render/Railway - mỗi service 1 web service, set biến môi trường trỏ URL public của
   nhau (tương tự cách `docker-compose.yml` đang trỏ qua tên container, chỉ đổi
   sang domain public + cập nhật `ocelot.Docker.json` cho khớp).

## Ghi chú kỹ thuật

- Database dùng **EF Core Migrations** (`Database.Migrate()` chạy tự động khi app khởi
  động) - không dùng `EnsureCreated()`, thể hiện quy trình chuẩn, cộng điểm Merit.
- `Options` trong entity `Poll` là `string[]` trong C#, nhưng SQL Server không hỗ trợ kiểu
  mảng như Postgres - EF Core value converter trong `PollDbContext.cs` tự chuyển nó thành
  JSON khi lưu và ngược lại khi đọc, hoàn toàn trong suốt với phần code còn lại.
- Local dev dùng LocalDB (built-in với Visual Studio), Docker dùng SQL Server container -
  chỉ khác connection string, không phải sửa code khi chuyển qua lại.

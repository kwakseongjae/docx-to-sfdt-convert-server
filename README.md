# DocumentEditor Server

ASP.NET Core 기반 Syncfusion DocumentEditor 서버

DOCX ↔ SFDT 변환 API를 제공합니다.

---

## 기능

- ✅ DOCX → SFDT 변환 (Import)
- ✅ SFDT → DOCX 변환 (Export)
- ✅ Base64 지원
- ✅ CORS 설정 (Next.js 연동)
- ✅ Docker 컨테이너화

---

## 수동 설치가 필요한 것

### 1. .NET 8.0 SDK 설치

**macOS (Homebrew)**:
```bash
brew install dotnet-sdk
```

**macOS (공식 설치 파일)**:
https://dotnet.microsoft.com/download/dotnet/8.0

**설치 확인**:
```bash
dotnet --version
# 8.0.x 출력되면 성공
```

### 2. Syncfusion 라이선스 키 설정

**커뮤니티 라이선스 발급** (무료, 연 매출 $1M 이하):
1. https://www.syncfusion.com/account/manage-trials/downloads 접속
2. 회원가입 / 로그인
3. Community License 신청
4. 라이선스 키 복사

**라이선스 키 설정 (로컬 개발)**:
```bash
# 방법 1: 환경 변수 (권장)
export SYNCFUSION_LICENSE_KEY="your_license_key_here"

# 방법 2: appsettings.json (Git에 푸시하지 마세요!)
cp appsettings.Example.json appsettings.json
# appsettings.json 파일 수정
# "LicenseKey": "YOUR_SYNCFUSION_LICENSE_KEY_HERE"
# 위 부분에 발급받은 키 붙여넣기
```

**⚠️ 보안 주의사항**:
- `appsettings.json`은 `.gitignore`에 포함되어 있어 Git에 푸시되지 않습니다
- `appsettings.Example.json`은 템플릿이므로 라이선스 키를 넣지 마세요
- 프로덕션 환경에서는 환경 변수를 사용하세요

---

## 로컬 실행

### 1. 의존성 설치
```bash
cd documenteditor-server
dotnet restore
```

### 2. 라이선스 키 설정
```bash
# 환경 변수로 설정 (권장)
export SYNCFUSION_LICENSE_KEY="your_license_key_here"

# 또는 appsettings.json 사용
cp appsettings.Example.json appsettings.json
# appsettings.json 파일 열어서 라이선스 키 입력
```

### 3. 개발 서버 실행
```bash
dotnet run
```

서버가 `http://localhost:5000`에서 실행됩니다.

### 3. API 테스트

**Health Check**:
```bash
curl http://localhost:5000/health
```

**DOCX → SFDT 변환** (base64):
```bash
curl -X POST http://localhost:5000/api/DocumentEditor/ImportBase64 \
  -H "Content-Type: application/json" \
  -d '{"content":"base64_encoded_docx_here"}'
```

---

## Docker 실행

### 1. Docker 이미지 빌드
```bash
docker build -t documenteditor-server .
```

### 2. Docker 컨테이너 실행
```bash
docker run -p 8080:8080 \
  -e SYNCFUSION_LICENSE_KEY="your_license_key" \
  documenteditor-server
```

서버가 `http://localhost:8080`에서 실행됩니다.

---

## 배포 (Railway)

### Railway란?
- 간단한 배포 플랫폼
- Docker 지원
- 무료 티어: $5/월 크레딧
- URL: https://railway.app

### 배포 단계

#### 1. Railway 계정 생성
https://railway.app 접속 → GitHub로 로그인

#### 2. GitHub Repository 생성
```bash
cd documenteditor-server
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/YOUR_USERNAME/documenteditor-server.git
git push -u origin main
```

#### 3. Railway에서 배포
1. Railway 대시보드 → "New Project"
2. "Deploy from GitHub repo" 선택
3. `documenteditor-server` 레포지토리 선택
4. 자동으로 Dockerfile 감지 및 배포 시작

#### 4. 환경 변수 설정
Railway 대시보드 → 프로젝트 → "Variables" 탭:
```
SYNCFUSION_LICENSE_KEY=your_license_key_here
ASPNETCORE_ENVIRONMENT=Production
```

#### 5. 배포 완료
Railway가 자동으로 URL 발급 (예: `https://documenteditor-server-production.up.railway.app`)

---

## 배포 (Fly.io)

### Fly.io란?
- 글로벌 엣지 배포
- 무료 티어: 3개 VM (256MB RAM)
- URL: https://fly.io

### 배포 단계

#### 1. Fly CLI 설치 (macOS)
```bash
brew install flyctl
```

#### 2. 로그인
```bash
fly auth login
```

#### 3. 앱 생성
```bash
cd documenteditor-server
fly launch
```

대화형 프롬프트에서:
- App name: `documenteditor-server` (또는 원하는 이름)
- Region: `nrt` (Tokyo) 선택
- PostgreSQL/Redis: No
- Deploy now: Yes

#### 4. 환경 변수 설정
```bash
fly secrets set SYNCFUSION_LICENSE_KEY="your_license_key_here"
```

#### 5. 배포 완료
URL: `https://documenteditor-server.fly.dev`

---

## Next.js 연동

### 1. serviceUrl 설정

**syncfusion-editor.tsx 수정**:
```typescript
<DocumentEditorContainerComponent
  serviceUrl="https://your-server.railway.app/api/DocumentEditor/"
  // 또는
  serviceUrl="https://documenteditor-server.fly.dev/api/DocumentEditor/"
  locale="ko-KR"
  enableTrackChanges={false}
/>
```

### 2. CORS 도메인 추가

**Program.cs에 프로덕션 도메인 추가**:
```csharp
policy.WithOrigins(
    "http://localhost:3000",
    "https://www.k-startup.ai",  // 이미 추가됨
    "https://your-custom-domain.com"  // 필요시 추가
)
```

### 3. 배포 후 재시작
```bash
# Railway: 자동 재배포 (git push)
git push origin main

# Fly.io: 수동 재배포
fly deploy
```

---

## API 엔드포인트

### POST /api/DocumentEditor/Import
DOCX 파일 업로드 → SFDT 반환

**Request**:
```
Content-Type: multipart/form-data
file: [DOCX file]
```

**Response**:
```json
{
  "sections": [...],
  "characterFormat": {...}
}
```

### POST /api/DocumentEditor/ImportBase64
Base64 DOCX → SFDT 반환

**Request**:
```json
{
  "content": "base64_encoded_docx",
  "fileName": "document.docx"
}
```

### POST /api/DocumentEditor/Export
SFDT → DOCX 파일 다운로드

**Request**:
```json
{
  "sfdt": "{...SFDT JSON...}",
  "fileName": "output.docx"
}
```

**Response**: DOCX 파일 (binary)

### POST /api/DocumentEditor/ExportBase64
SFDT → Base64 DOCX 반환

**Request**:
```json
{
  "sfdt": "{...SFDT JSON...}",
  "fileName": "output.docx"
}
```

**Response**:
```json
{
  "content": "base64_encoded_docx",
  "fileName": "output.docx"
}
```

---

## 비용 예상

### Railway
- 무료 티어: $5/월 크레딧
- Hobby Plan: $5/월 (충분)
- 예상 사용량: ~$3-5/월

### Fly.io
- 무료 티어: 3 VMs (256MB RAM)
- 예상: 무료 티어로 충분

### Syncfusion
- Community License: 무료 (연 매출 $1M 이하)
- 이미 프론트엔드에서 사용 중이므로 추가 비용 없음

**총 예상 비용**: **$0-5/월**

---

## 트러블슈팅

### 1. .NET SDK 설치 확인
```bash
dotnet --version
```

### 2. 라이선스 에러
```
Syncfusion license key is invalid
```
→ appsettings.json의 라이선스 키 확인

### 3. CORS 에러
```
Access to fetch at ... has been blocked by CORS policy
```
→ Program.cs의 AllowedOrigins에 도메인 추가

### 4. Railway 배포 실패
- Dockerfile 문법 확인
- 환경 변수 설정 확인
- Railway 로그 확인: Dashboard → Deployments → Logs

---

## 다음 단계

1. ✅ .NET SDK 설치
2. ✅ Syncfusion 라이선스 발급 및 설정
3. ✅ 로컬 테스트 (`dotnet run`)
4. ✅ Docker 테스트 (선택)
5. ✅ GitHub에 푸시
6. ✅ Railway/Fly.io 배포
7. ✅ Next.js에서 serviceUrl 설정
8. ✅ 프로덕션 테스트

---

## 라이선스

- ASP.NET Core: MIT License
- Syncfusion: Community License 필요 (무료)

---

**제작**: 2026-01-28
**업데이트**: 2026-01-28

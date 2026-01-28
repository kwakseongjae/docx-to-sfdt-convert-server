# Next.js 프로젝트와 연동하기

이 가이드는 `business_plan_k` Next.js 프로젝트에서 DocumentEditor 서버를 사용하는 방법을 설명합니다.

---

## 전제 조건

- ✅ DocumentEditor 서버가 배포되어 있음 (Railway 또는 Fly.io)
- ✅ 서버 URL을 알고 있음 (예: `https://documenteditor-server.railway.app`)

---

## 1. syncfusion-editor.tsx 수정

### Before (mammoth.js 사용)
```typescript
// 기존 코드 - mammoth.js로 DOCX를 HTML로 변환
useEffect(() => {
  const loadDocx = async () => {
    const response = await fetch("/api/load-docx")
    const result = await response.json()
    const mammothResult = await mammoth.convertToHtml(...)
    editor.editor.paste(mammothResult.value)
  }
  loadDocx()
}, [])
```

### After (서버 API 사용)
```typescript
// 새 코드 - 서버 API로 DOCX를 SFDT로 변환
useEffect(() => {
  const loadDocx = async () => {
    // 1. DOCX를 base64로 로드
    const response = await fetch("/api/load-docx")
    const result = await response.json()
    const base64 = result.data

    // 2. 서버에 DOCX → SFDT 변환 요청
    const convertResponse = await fetch(
      "https://YOUR_SERVER_URL/api/DocumentEditor/ImportBase64",
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content: base64 })
      }
    )

    const sfdt = await convertResponse.text()

    // 3. SFDT를 DocumentEditor에 로드
    editorRef.current.documentEditor.open(sfdt)
  }
  loadDocx()
}, [])
```

### 완전한 코드

```typescript
"use client"

import { useRef, useEffect } from "react"
import type { DocumentEditorContainerComponent as DocEditorType } from "@syncfusion/ej2-react-documenteditor"
import {
  DocumentEditorContainerComponent,
  Toolbar,
  Ribbon,
} from "@syncfusion/ej2-react-documenteditor"
import { L10n } from "@syncfusion/ej2-base"

// Inject services
DocumentEditorContainerComponent.Inject(Toolbar, Ribbon)

// Korean locale (기존 코드 유지)
L10n.load({ "ko-KR": { ... } })

export function SyncfusionEditor() {
  const editorRef = useRef<DocEditorType>(null)

  if (typeof window === "undefined") {
    return null
  }

  // DOCX 파일 로드 및 SFDT 변환
  useEffect(() => {
    const loadDocx = async () => {
      if (!editorRef.current) return

      try {
        console.log("📄 DOCX 파일 로드 시작...")

        // 1. DOCX를 base64로 로드
        const response = await fetch("/api/load-docx")
        if (!response.ok) throw new Error(`API 요청 실패: ${response.status}`)

        const result = await response.json()
        if (!result.success) throw new Error(result.error || "DOCX 로드 실패")

        console.log("🔄 DOCX → SFDT 변환 중...")

        // 2. 서버에 DOCX → SFDT 변환 요청
        const convertResponse = await fetch(
          `${process.env.NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL}/api/DocumentEditor/ImportBase64`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ content: result.data })
          }
        )

        if (!convertResponse.ok) {
          throw new Error(`변환 실패: ${convertResponse.status}`)
        }

        const sfdt = await convertResponse.text()

        // 3. SFDT를 DocumentEditor에 로드
        console.log("✅ SFDT 로드 중...")
        editorRef.current.documentEditor.open(sfdt)

        console.log("✅ DOCX 파일 로드 완료!")
      } catch (error) {
        console.error("❌ DOCX 로드 실패:", error)
      }
    }

    const timer = setTimeout(loadDocx, 500)
    return () => clearTimeout(timer)
  }, [])

  return (
    <div className="h-screen w-full">
      <DocumentEditorContainerComponent
        id="document-editor"
        ref={editorRef}
        style={{ display: "block" }}
        height="100%"
        enableToolbar={true}
        toolbarMode="Ribbon"
        ribbonLayout="Classic"
        serviceUrl={`${process.env.NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL}/api/DocumentEditor/`}
        locale="ko-KR"
        enableTrackChanges={false}
        restrictEditing={false}
      />
    </div>
  )
}
```

---

## 2. 환경 변수 설정

### .env.local 생성/수정

`business_plan_k/.env.local`:
```bash
# DocumentEditor 서버 URL
NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL=https://your-server.railway.app

# 또는 Fly.io
# NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL=https://documenteditor-server.fly.dev

# 또는 로컬 개발
# NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL=http://localhost:5000
```

---

## 3. 저장 (Export) 기능 추가

### 저장 버튼 추가

```typescript
const handleSave = async () => {
  if (!editorRef.current) return

  try {
    // 1. SFDT 추출
    const sfdt = editorRef.current.documentEditor.serialize()

    // 2. 서버에 SFDT → DOCX 변환 요청
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL}/api/DocumentEditor/Export`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sfdt: sfdt,
          fileName: "사업계획서.docx"
        })
      }
    )

    if (!response.ok) throw new Error("저장 실패")

    // 3. DOCX 파일 다운로드
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = "사업계획서.docx"
    a.click()
    URL.revokeObjectURL(url)

    console.log("✅ 저장 완료!")
  } catch (error) {
    console.error("❌ 저장 실패:", error)
  }
}
```

---

## 4. 테스트

### 로컬 테스트 (서버가 localhost:5000에서 실행 중)

```bash
# Terminal 1: DocumentEditor 서버 실행
cd documenteditor-server
dotnet run

# Terminal 2: Next.js 실행
cd business_plan_k
npm run dev
```

### 프로덕션 테스트

1. 서버가 배포되어 있는지 확인:
   ```bash
   curl https://your-server.railway.app/health
   ```

2. Next.js에서 `.env.local`에 서버 URL 설정

3. Next.js 실행:
   ```bash
   npm run dev
   ```

4. 브라우저에서 http://localhost:3000/write/preliminary-test 접속

5. 콘솔에서 확인:
   ```
   📄 DOCX 파일 로드 시작...
   🔄 DOCX → SFDT 변환 중...
   ✅ SFDT 로드 중...
   ✅ DOCX 파일 로드 완료!
   ```

---

## 5. 프로덕션 배포

### Vercel 환경 변수 설정

1. Vercel Dashboard → 프로젝트 → Settings → Environment Variables
2. 추가:
   ```
   NEXT_PUBLIC_DOCUMENTEDITOR_SERVER_URL=https://your-server.railway.app
   ```
3. Redeploy

---

## 트러블슈팅

### CORS 에러
```
Access to fetch at ... has been blocked by CORS policy
```

**해결**:
DocumentEditor 서버의 `Program.cs`에 도메인 추가:
```csharp
policy.WithOrigins(
    "http://localhost:3000",
    "https://www.k-startup.ai",
    "https://your-vercel-domain.vercel.app"  // 추가
)
```

### 변환 실패
```
❌ DOCX 로드 실패: Error: 변환 실패: 500
```

**원인**: Syncfusion 라이선스 키 누락 또는 잘못됨

**해결**: 서버의 `appsettings.json` 확인

### 로딩 느림
**원인**: 서버가 Cold Start (Railway/Fly.io 무료 티어)

**해결**:
- 유료 플랜으로 업그레이드 (항상 켜져 있음)
- 또는 로딩 인디케이터 추가

---

## 비교: mammoth.js vs 서버 API

| | mammoth.js | 서버 API |
|---|---|---|
| **서식 유지** | ⚠️ 일부 손실 | ✅ 완벽 |
| **표 지원** | ⚠️ 단순화 | ✅ 완벽 |
| **이미지** | ⚠️ 제한적 | ✅ 완벽 |
| **속도** | ✅ 빠름 | ⚠️ 약간 느림 |
| **비용** | ✅ 무료 | ⚠️ $5/월 |
| **인프라** | ✅ 불필요 | ⚠️ 서버 필요 |

**결론**: 완벽한 DOCX 지원이 필요하면 서버 API 사용

---

**업데이트**: 2026-01-28

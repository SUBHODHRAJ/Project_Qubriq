import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { registrationService } from "../services/registrationService";
import "../styles/auth.css";
export default function SignupPage() {
    const [types, setTypes] = useState([]);
    const [type, setType] = useState("");
    const [form, setForm] = useState({ fullName:"", email:"", password:"", companyName:"", contactName:"", phone:"", address:"" });
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    useEffect(() => {
        registrationService.publicPolicy().then(p => {
            setTypes(p.accountTypes || []);
            setType(p.accountTypes?.[0] || "");
        }).catch(e => setError(e.message)).finally(() => setLoading(false));
    }, []);
    const update = (key, value) => setForm(v => ({ ...v, [key]:value }));
    async function submit(e) {
        e.preventDefault(); setError(""); setMessage(""); setBusy(true);
        try {
            const result = await registrationService.register({ ...form, accountType:type });
            setMessage(result.message);
        } catch (e) { setError(e.message || "Registration failed."); }
        finally { setBusy(false); }
    }
    return <main className="auth-page"><section className="auth-card">
        <Link to="/" className="auth-brand">Qubriq</Link><p className="auth-eyebrow">CREATE YOUR ACCOUNT</p>
        <h1>Join Qubriq</h1><p className="auth-description">Create your Qubriq account.</p>
        {loading && <p>Loading registration options...</p>}
        {!loading && types.length === 0 && <div className="auth-information">Public registration is unavailable. Contact your administrator.</div>}
        {!loading && types.length > 0 && !message && <form className="auth-form" onSubmit={submit}>
            <label>Account type<select value={type} onChange={e=>setType(e.target.value)}>{types.map(t=><option key={t} value={t}>{t==="BookingCustomer"?"Booking Customer":"Final Recipient"}</option>)}</select></label>
            <label>Full name<input required maxLength={150} value={form.fullName} onChange={e=>update("fullName",e.target.value)} /></label>
            <label>Email<input required type="email" maxLength={254} value={form.email} onChange={e=>update("email",e.target.value)} /></label>
            <label>Password<input required type="password" minLength={12} maxLength={128} value={form.password} onChange={e=>update("password",e.target.value)} /></label>
            {type==="BookingCustomer" && <>
                <label>Company name<input required maxLength={200} value={form.companyName} onChange={e=>update("companyName",e.target.value)} /></label>
                <label>Contact name<input maxLength={150} value={form.contactName} onChange={e=>update("contactName",e.target.value)} /></label>
                <label>Phone<input maxLength={30} value={form.phone} onChange={e=>update("phone",e.target.value)} /></label>
                <label>Address<textarea rows={3} maxLength={500} value={form.address} onChange={e=>update("address",e.target.value)} /></label>
            </>}
            {error && <div className="auth-error" role="alert">{error}</div>}
            <button disabled={busy}>{busy?"Creating account...":"Create account"}</button>
        </form>}
        {message && <div className="auth-information" role="status">{message}</div>}
        <p className="auth-footer">Already registered? <Link to="/login">Sign in</Link></p>
        <Link to="/" className="auth-back-link">Back to home</Link>
    </section></main>;
}

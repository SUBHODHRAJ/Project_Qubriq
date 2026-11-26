import { useEffect, useState } from "react";
import { registrationService } from "../services/registrationService";
const defaults = { publicRegistrationEnabled:true, bookingCustomerSignupEnabled:true, finalRecipientSignupEnabled:false, requireAdminApproval:false, activatePublicAccountsImmediately:true, adminAccountCreationEnabled:true };
const fields = [
 ["publicRegistrationEnabled","Allow public registration"],
 ["bookingCustomerSignupEnabled","Booking Customer signup"],
 ["finalRecipientSignupEnabled","Final Recipient signup"],
 ["requireAdminApproval","Require Admin approval"],
 ["activatePublicAccountsImmediately","Activate public accounts immediately"],
 ["adminAccountCreationEnabled","Allow Admin account creation"]
];
export default function AdminSettingsPage() {
 const [policy,setPolicy]=useState(defaults), [pending,setPending]=useState([]), [error,setError]=useState(""), [message,setMessage]=useState(""), [busy,setBusy]=useState(false);
 async function load() {
  try { const [p,u]=await Promise.all([registrationService.adminPolicy(),registrationService.pending()]); setPolicy(Object.fromEntries(fields.map(([k])=>[k,Boolean(p[k])]))); setPending(u||[]); }
  catch(e){setError(e.message||"Unable to load settings.");}
 }
 useEffect(()=>{load();},[]);
 async function save(e){e.preventDefault();setBusy(true);setError("");setMessage("");try{await registrationService.savePolicy(policy);setMessage("Registration policy saved.");await load();}catch(e){setError(e.message);}finally{setBusy(false);}}
 async function approve(id){setError("");try{await registrationService.approve(id);setMessage("Account approved.");await load();}catch(e){setError(e.message);}}
 return <section className="module-page">
  <div className="page-heading-row"><div><h1>Account & Registration Settings</h1><p>Manage public signup rules and pending account approvals.</p></div></div>
  <form className="dashboard-card" onSubmit={save}><h2>Registration policy</h2><p>Updates are stored in MySQL and apply to subsequent registration requests.</p>
   <div style={{display:"grid",gap:16,margin:"20px 0"}}>{fields.map(([key,label])=><label key={key} style={{display:"flex",gap:12,alignItems:"center"}}><input type="checkbox" checked={policy[key]} onChange={e=>setPolicy(p=>({...p,[key]:e.target.checked}))}/><strong>{label}</strong></label>)}</div>
   <button disabled={busy}>{busy?"Saving...":"Save settings"}</button>
  </form>
  <div className="dashboard-card" style={{marginTop:20}}><h2>Pending approvals</h2>
   {pending.length===0?<p>No inactive accounts found.</p>:<div style={{overflowX:"auto"}}><table style={{width:"100%"}}><thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Created</th><th></th></tr></thead><tbody>{pending.map(u=><tr key={u.id}><td>{u.fullName}</td><td>{u.email}</td><td>{(u.roles||[]).join(", ")}</td><td>{new Date(u.createdAtUtc).toLocaleString()}</td><td><button type="button" onClick={()=>approve(u.id)}>Approve</button></td></tr>)}</tbody></table></div>}
  </div>
  {message&&<p role="status">{message}</p>}{error&&<p role="alert" style={{color:"#b42318"}}>{error}</p>}
 </section>;
}

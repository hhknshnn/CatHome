'use strict';
const data=JSON.parse(document.getElementById('data').textContent);
const viewer=document.getElementById('viewer');
const viewerImage=document.getElementById('viewer-image');
const viewerBody=document.getElementById('viewer-body');
let current=0;
function showImage(index){
 current=(index+data.length)%data.length;
 const item=data[current];
 viewerImage.src='images/'+item.file;
 viewerImage.alt=item.title+' tasarım panosu';
 document.getElementById('viewer-title').textContent=item.id+' / '+item.title;
 document.getElementById('viewer-note').textContent=item.note;
 document.getElementById('viewer-download').href=viewerImage.src;
 viewerBody.classList.remove('zoom');
 document.getElementById('zoom').textContent='Gerçek boyut';
 if(!viewer.open){viewer.showModal();document.body.classList.add('locked');}
}
document.querySelectorAll('.zoom-link').forEach(link=>link.addEventListener('click',event=>{
 event.preventDefault();showImage(data.findIndex(item=>item.id===link.dataset.id));
}));
document.getElementById('close-viewer').addEventListener('click',()=>viewer.close());
viewer.addEventListener('close',()=>document.body.classList.remove('locked'));
document.getElementById('prev').addEventListener('click',()=>showImage(current-1));
document.getElementById('next').addEventListener('click',()=>showImage(current+1));
document.getElementById('zoom').addEventListener('click',()=>{
 const zoomed=viewerBody.classList.toggle('zoom');
 document.getElementById('zoom').textContent=zoomed?'Ekrana sığdır':'Gerçek boyut';
});
document.addEventListener('keydown',event=>{
 if(!viewer.open)return;
 if(event.key==='ArrowLeft'){event.preventDefault();showImage(current-1);}
 if(event.key==='ArrowRight'){event.preventDefault();showImage(current+1);}
});
document.querySelectorAll('[data-filter]').forEach(button=>button.addEventListener('click',()=>{
 document.querySelectorAll('[data-filter]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
 let count=0;
 document.querySelectorAll('.board').forEach(card=>{
  card.hidden=button.dataset.filter!=='all'&&card.dataset.group!==button.dataset.filter;
  if(!card.hidden)count++;
 });
 document.getElementById('count').textContent=count+' pano';
}));

const kind=document.getElementById('motion-kind');
const reduced=document.getElementById('reduced');
const sample=document.getElementById('sample');
const scrub=document.getElementById('motion-time');
const timeLabel=document.getElementById('time-label');
const motionLabel=document.getElementById('motion-label');
let animation=null,raf=0,total=1500;
reduced.checked=window.matchMedia('(prefers-reduced-motion: reduce)').matches;
function setTime(time){
 scrub.value=String(time);timeLabel.textContent=Math.round(time)+' ms';
 const coin=sample.querySelector('.coin');
 if(coin)coin.textContent='+'+(reduced.checked?100:Math.min(100,Math.round(Math.max(0,time-160)/650*100)));
}
function configureMotion(preview=true){
 cancelAnimationFrame(raf);
 if(animation)animation.cancel();
 const key=kind.value;
 sample.className='sample '+key;
 const templates={
  panel:'<h3>Birlikte</h3><p>Küçük bir mola verelim mi?</p><span class="chip">Otur</span>',
  bubble:'<h3>Mırr…</h3><p>Yanında olmak güzel.</p>',
  toast:'<h3>Görev tamamlandı</h3><p>Runner oyna</p>',
  reward:'<h3>Seviye 31!</h3><span class="coin">+100</span><p>Jeton · örnek kazanım</p><span class="chip">Devam et</span>'
 };
 sample.innerHTML=templates[key];
 let entry=200,exit=140;
 if(key==='bubble'){entry=180;exit=140;total=2500;}
 else if(key==='toast'){entry=180;exit=160;total=2540;}
 else if(key==='reward'){entry=200;exit=140;total=1800;}
 else total=1500;
 if(reduced.checked){entry=100;exit=100;}
 const base=key==='toast'?'translate(0,0)':'translate(-50%,-50%)';
 const offset=reduced.checked?base:(key==='toast'?'translateX(16px)':base+' translateY(12px) scale(.98)');
 animation=sample.animate([
  {opacity:0,transform:offset,offset:0},
  {opacity:1,transform:base,offset:entry/total},
  {opacity:1,transform:base,offset:(total-exit)/total},
  {opacity:0,transform:reduced.checked?base:base+' translateY(-6px)',offset:1}
 ],{duration:total,easing:'linear',fill:'both'});
 animation.pause();animation.currentTime=preview?entry:0;
 scrub.max=String(total);setTime(Number(animation.currentTime));
 motionLabel.textContent=entry+' ms giriş · '+exit+' ms çıkış'+(reduced.checked?' · konum/ölçek hareketi kapalı':'');
}
function tick(){
 if(!animation)return;
 setTime(Number(animation.currentTime)||0);
 if(animation.playState==='running')raf=requestAnimationFrame(tick);
}
document.getElementById('play-motion').addEventListener('click',()=>{
 cancelAnimationFrame(raf);animation.currentTime=0;animation.play();tick();
});
document.getElementById('reset-motion').addEventListener('click',()=>{
 animation.pause();animation.currentTime=0;cancelAnimationFrame(raf);setTime(0);
});
scrub.addEventListener('input',()=>{
 animation.pause();cancelAnimationFrame(raf);animation.currentTime=Number(scrub.value);setTime(Number(scrub.value));
});
kind.addEventListener('change',()=>configureMotion());
reduced.addEventListener('change',()=>configureMotion());
document.addEventListener('visibilitychange',()=>{
 if(document.hidden&&animation){animation.pause();cancelAnimationFrame(raf);}
});
configureMotion();
